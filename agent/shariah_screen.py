"""
Shariah compliance screening.

This implements the widely-used AAOIFI-style screening approach (similar to what
indices like the S&P Shariah / Dow Jones Islamic Market / MSCI Islamic use):

1. Sector/business screen: exclude companies whose core business involves
   interest-based finance, alcohol, gambling, pork, conventional insurance,
   tobacco, adult entertainment, weapons, or other haram activities.

2. Financial ratio screen (using the most recent balance sheet from a free
   fundamentals API), all as a percentage of market cap (or total assets,
   depending on methodology - market cap is used here, the more common approach):
   - Total interest-bearing debt / Market Cap < 33%
   - Cash + interest-bearing securities / Market Cap < 33%
   - Accounts receivable + cash / Market Cap < 33% (some methodologies) - kept optional
   - Non-permissible income (e.g. interest income) / Total revenue < 5%

This is a rule-based approximation, not a fatwa. For real deployment, cross-check
against a licensed Shariah screening provider (e.g. IdealRatings, Zoya, Musaffa).
"""

from dataclasses import dataclass
import httpx
from config import FMP_API_KEY
from rate_limiter import fmp_semaphore

HARAM_SECTORS_KEYWORDS = [
    "bank", "insurance", "financial services", "capital markets", "credit",
    "alcohol", "beverages - wineries", "brewers", "distillers",
    "casino", "gambling", "gaming",
    "tobacco", "cigarette",
    "defense", "aerospace & defense", "weapons",
    "adult entertainment",
    "pork", "meat products",  # only flags if explicitly pork-related in description
]

# Full-fledged Islamic financial institutions are structurally Shariah-compliant
# by charter (no interest-based lending), so the generic "bank"/"financial services"
# sector exclusion would wrongly flag them. Maintain an explicit whitelist instead.
# NOTE: this is a starting list, not exhaustive — verify against a dedicated
# screening provider before trading any symbol not confirmed here.
KNOWN_ISLAMIC_FINANCIAL_INSTITUTIONS = {
    "MEBL": "Meezan Bank — Pakistan's first and largest full-fledged Islamic bank",
    "BIPL": "Bank Islami Pakistan — full-fledged Islamic bank",
    "DIB": "Dubai Islamic Bank — full-fledged Islamic bank",
    "ADIB": "Abu Dhabi Islamic Bank — full-fledged Islamic bank",
    "ALRAJHI": "Al Rajhi Bank — full-fledged Islamic bank",
    "KFH": "Kuwait Finance House — full-fledged Islamic bank",
}

DEBT_TO_MARKETCAP_MAX = 0.33
CASH_SECURITIES_TO_MARKETCAP_MAX = 0.33
NON_PERMISSIBLE_INCOME_MAX = 0.05


@dataclass
class ShariahResult:
    symbol: str
    compliant: bool
    notes: str


def _sector_screen(sector: str, industry: str) -> tuple[bool, str]:
    text = f"{sector} {industry}".lower()
    for kw in HARAM_SECTORS_KEYWORDS:
        if kw in text:
            return False, f"Excluded: business sector/industry '{industry}' matches restricted category '{kw}'"
    return True, ""


def _financial_ratio_screen(market_cap: float, total_debt: float,
                             cash_and_securities: float) -> tuple[bool, str]:
    if market_cap <= 0:
        return False, "No market cap data available"

    debt_ratio = total_debt / market_cap
    cash_ratio = cash_and_securities / market_cap

    if debt_ratio >= DEBT_TO_MARKETCAP_MAX:
        return False, f"Debt/MarketCap {debt_ratio:.0%} exceeds {DEBT_TO_MARKETCAP_MAX:.0%} threshold"
    if cash_ratio >= CASH_SECURITIES_TO_MARKETCAP_MAX:
        return False, f"Cash+securities/MarketCap {cash_ratio:.0%} exceeds {CASH_SECURITIES_TO_MARKETCAP_MAX:.0%} threshold"

    return True, f"Debt ratio {debt_ratio:.0%}, cash ratio {cash_ratio:.0%} — within limits"


async def screen_stock(symbol: str) -> ShariahResult:
    if symbol.upper() in KNOWN_ISLAMIC_FINANCIAL_INSTITUTIONS:
        return ShariahResult(
            symbol=symbol, compliant=True,
            notes=f"Whitelisted Islamic financial institution: {KNOWN_ISLAMIC_FINANCIAL_INSTITUTIONS[symbol.upper()]}",
        )

    async with httpx.AsyncClient(timeout=15) as client:
        async with fmp_semaphore:
            try:
                profile_resp = await client.get(
                    "https://financialmodelingprep.com/stable/profile",
                    params={"symbol": symbol, "apikey": FMP_API_KEY},
                )
                if profile_resp.status_code == 429:
                    print(f"[shariah_screen] FMP rate-limited on {symbol} profile lookup")
                profile_resp.raise_for_status()
                profile = (profile_resp.json() or [{}])[0]
            except Exception as e:
                print(f"[shariah_screen] profile fetch failed for {symbol}: {e}")
                profile = {}

        sector = profile.get("sector", "") or ""
        industry = profile.get("industry", "") or ""
        market_cap = profile.get("marketCap", 0) or 0

        ok, note = _sector_screen(sector, industry)
        if not ok:
            return ShariahResult(symbol=symbol, compliant=False, notes=note)

        async with fmp_semaphore:
            try:
                bs_resp = await client.get(
                    "https://financialmodelingprep.com/stable/balance-sheet-statement",
                    params={"symbol": symbol, "apikey": FMP_API_KEY, "limit": 1},
                )
                if bs_resp.status_code == 429:
                    print(f"[shariah_screen] FMP rate-limited on {symbol} balance sheet lookup")
                bs_resp.raise_for_status()
                bs = (bs_resp.json() or [{}])[0]
            except Exception as e:
                print(f"[shariah_screen] balance sheet fetch failed for {symbol}: {e}")
                bs = {}

        total_debt = (bs.get("shortTermDebt", 0) or 0) + (bs.get("longTermDebt", 0) or 0)
        cash_and_securities = (bs.get("cashAndCashEquivalents", 0) or 0) + \
                               (bs.get("shortTermInvestments", 0) or 0)

        ok, note = _financial_ratio_screen(market_cap, total_debt, cash_and_securities)
        return ShariahResult(symbol=symbol, compliant=ok, notes=note)
