"""
Pulls recent news headlines for a symbol/company (via NewsAPI's free tier)
and scores sentiment with VADER (lightweight, no API key, good enough for
headline-level polarity). Also pulls a couple of key fundamentals ratios
from Financial Modeling Prep's free tier to factor into the decision.
"""

import httpx
from vaderSentiment.vaderSentiment import SentimentIntensityAnalyzer
from config import NEWSAPI_KEY, FMP_API_KEY
from rate_limiter import fmp_semaphore

_analyzer = SentimentIntensityAnalyzer()


async def get_news_sentiment(company_query: str) -> dict:
    if not NEWSAPI_KEY:
        return {"score": 0.0, "headlines": [], "detail": "NEWSAPI_KEY not configured"}

    async with httpx.AsyncClient(timeout=15) as client:
        try:
            resp = await client.get(
                "https://newsapi.org/v2/everything",
                params={
                    "q": company_query,
                    "language": "en",
                    "sortBy": "publishedAt",
                    "pageSize": 10,
                    "apiKey": NEWSAPI_KEY,
                },
            )
            data = resp.json()
        except Exception as e:
            return {"score": 0.0, "headlines": [], "detail": f"news fetch failed: {e}"}

    articles = data.get("articles", [])
    if not articles:
        return {"score": 0.0, "headlines": [], "detail": "no recent articles found"}

    scores = []
    headlines = []
    for a in articles:
        title = a.get("title") or ""
        if not title:
            continue
        compound = _analyzer.polarity_scores(title)["compound"]
        scores.append(compound)
        headlines.append(title)

    avg = sum(scores) / len(scores) if scores else 0.0
    return {"score": round(avg, 3), "headlines": headlines[:5], "detail": f"{len(scores)} articles scored"}


async def get_fundamentals_snapshot(symbol: str) -> dict:
    """A very simple fundamentals health score in -1..1, using free FMP ratios."""
    if not FMP_API_KEY:
        return {"score": 0.0, "detail": "FMP_API_KEY not configured"}

    async with httpx.AsyncClient(timeout=15) as client:
        async with fmp_semaphore:
            try:
                resp = await client.get(
                    "https://financialmodelingprep.com/stable/ratios-ttm",
                    params={"symbol": symbol, "apikey": FMP_API_KEY},
                )
                if resp.status_code == 429:
                    print(f"[news_analysis] FMP rate-limited on {symbol} ratios lookup")
                resp.raise_for_status()
                ratios = (resp.json() or [{}])[0]
            except Exception as e:
                print(f"[news_analysis] fundamentals fetch failed for {symbol}: {e}")
                return {"score": 0.0, "detail": f"fundamentals fetch failed: {e}"}

    pe = ratios.get("priceToEarningsRatioTTM", 0) or 0
    roe = ratios.get("returnOnEquityTTM", 0) or 0
    current_ratio = ratios.get("currentRatioTTM", 0) or 0

    score = 0.0
    if 0 < pe < 25:
        score += 0.3
    if roe > 0.10:
        score += 0.4
    if current_ratio > 1.2:
        score += 0.3

    score = max(min(score, 1.0), -1.0)
    return {
        "score": round(score, 3),
        "detail": f"PE {pe:.1f}, ROE {roe:.1%}, current ratio {current_ratio:.2f}",
    }
