import os
from dotenv import load_dotenv

load_dotenv()

# --- API keys (all free-tier options) ---
# NewsAPI (https://newsapi.org) - free dev tier: 100 requests/day, headlines + search
NEWSAPI_KEY = os.getenv("NEWSAPI_KEY", "")

# Financial Modeling Prep (https://financialmodelingprep.com) - free tier gives
# balance sheet / income statement / ratios, used for Shariah financial screening
FMP_API_KEY = os.getenv("FMP_API_KEY", "")

# Alpaca (https://alpaca.markets) - free paper trading + market data
ALPACA_API_KEY_ID = os.getenv("ALPACA_API_KEY_ID", "")
ALPACA_API_SECRET_KEY = os.getenv("ALPACA_API_SECRET_KEY", "")
ALPACA_BASE_URL = os.getenv("ALPACA_BASE_URL", "https://paper-api.alpaca.markets")
ALPACA_DATA_URL = os.getenv("ALPACA_DATA_URL", "https://data.alpaca.markets")

# Local Ollama (no API key needed)
OLLAMA_MODEL = os.getenv("OLLAMA_MODEL", "qwen2.5:7b")
OLLAMA_BASE_URL = os.getenv("OLLAMA_BASE_URL", "http://localhost:11434")

# Risk appetite -> tunable parameters for the decision engine
RISK_PARAMS = {
    "Conservative": {"confidence_threshold": 0.75, "news_weight": 0.2, "fundamentals_weight": 0.6, "momentum_weight": 0.2},
    "Moderate":     {"confidence_threshold": 0.60, "news_weight": 0.35, "fundamentals_weight": 0.4, "momentum_weight": 0.25},
    "Aggressive":   {"confidence_threshold": 0.50, "news_weight": 0.5, "fundamentals_weight": 0.2, "momentum_weight": 0.3},
}
