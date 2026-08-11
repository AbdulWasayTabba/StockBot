"""
Combines the three signal sources (news sentiment, fundamentals, momentum)
into a single buy/sell/hold decision, weighted according to the user's risk
appetite. Shariah compliance is checked first and is an absolute gate:
non-compliant stocks are never recommended, no matter the score.
"""

from dataclasses import dataclass, field
from config import RISK_PARAMS
from shariah_screen import screen_stock
from news_analysis import get_news_sentiment, get_fundamentals_snapshot
from market_data import get_momentum_score


@dataclass
class Decision:
    symbol: str
    action: str  # "buy" | "sell" | "hold"
    confidence: float
    rationale: str
    shariah_compliant: bool


async def analyze_symbol(symbol: str, risk_appetite: str, company_query: str | None = None) -> Decision:
    params = RISK_PARAMS.get(risk_appetite, RISK_PARAMS["Moderate"])

    shariah = await screen_stock(symbol)
    if not shariah.compliant:
        return Decision(
            symbol=symbol, action="hold", confidence=0.0,
            rationale=f"Not Shariah-compliant: {shariah.notes}",
            shariah_compliant=False,
        )

    news = await get_news_sentiment(company_query or symbol)
    fundamentals = await get_fundamentals_snapshot(symbol)
    momentum = await get_momentum_score(symbol)

    combined = (
        news["score"] * params["news_weight"]
        + fundamentals["score"] * params["fundamentals_weight"]
        + momentum["score"] * params["momentum_weight"]
    )
    # combined is roughly in -1..1; map to a 0..1 confidence for the stronger side
    confidence = min(abs(combined), 1.0)

    if combined > 0.15 and confidence >= params["confidence_threshold"]:
        action = "buy"
    elif combined < -0.15 and confidence >= params["confidence_threshold"]:
        action = "sell"
    else:
        action = "hold"

    rationale = (
        f"news={news['score']:+.2f} ({news['detail']}); "
        f"fundamentals={fundamentals['score']:+.2f} ({fundamentals['detail']}); "
        f"momentum={momentum['score']:+.2f} ({momentum['detail']}); "
        f"risk_profile={risk_appetite}"
    )

    return Decision(
        symbol=symbol, action=action, confidence=round(confidence, 3),
        rationale=rationale, shariah_compliant=True,
    )
