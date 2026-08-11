"""
Market data & simple momentum indicators, using Alpaca's market data API
directly (same keys already configured for trading). Replaces the earlier
yfinance-based version, which broke intermittently due to Yahoo Finance's
anti-bot measures blocking unauthenticated requests.
"""

import httpx
from datetime import datetime, timedelta, timezone
from config import ALPACA_API_KEY_ID, ALPACA_API_SECRET_KEY, ALPACA_DATA_URL

_HEADERS = {
    "APCA-API-KEY-ID": ALPACA_API_KEY_ID,
    "APCA-API-SECRET-KEY": ALPACA_API_SECRET_KEY,
}


async def _get_daily_closes(symbol: str, days: int = 90) -> list[float]:
    start = (datetime.now(timezone.utc) - timedelta(days=days)).strftime("%Y-%m-%d")
    url = f"{ALPACA_DATA_URL}/v2/stocks/{symbol}/bars"
    params = {"timeframe": "1Day", "start": start, "limit": days, "adjustment": "raw", "feed": "iex"}

    async with httpx.AsyncClient(timeout=15) as client:
        resp = await client.get(url, headers=_HEADERS, params=params)
        if resp.status_code != 200:
            return []
        data = resp.json()
        bars = data.get("bars", [])
        return [b["c"] for b in bars]


def get_last_price(symbol: str) -> float | None:
    # Synchronous wrapper kept for compatibility with existing callers;
    # prefer get_momentum_score (async) for anything new.
    import asyncio
    try:
        closes = asyncio.get_event_loop().run_until_complete(_get_daily_closes(symbol, days=5))
    except RuntimeError:
        closes = asyncio.run(_get_daily_closes(symbol, days=5))
    return closes[-1] if closes else None


async def get_momentum_score(symbol: str) -> dict:
    """Returns a -1..1 momentum score plus the raw stats used to derive it."""
    closes = await _get_daily_closes(symbol, days=90)
    if len(closes) < 20:
        return {"score": 0.0, "detail": "insufficient price history"}

    last = closes[-1]
    sma20 = sum(closes[-20:]) / 20
    window50 = closes[-50:] if len(closes) >= 50 else closes
    sma50 = sum(window50) / len(window50)
    pct_change_1mo = (last - closes[-21]) / closes[-21] if len(closes) > 21 else 0.0

    score = 0.0
    if last > sma20:
        score += 0.3
    if last > sma50:
        score += 0.3
    score += max(min(pct_change_1mo * 2, 0.4), -0.4)

    score = max(min(score, 1.0), -1.0)
    return {
        "score": round(score, 3),
        "detail": f"price {last:.2f} vs SMA20 {sma20:.2f} / SMA50 {sma50:.2f}, "
                  f"1mo change {pct_change_1mo:.1%}",
    }
