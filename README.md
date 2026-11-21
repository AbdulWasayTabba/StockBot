# StockApp — Shariah-Compliant Trading Assistant

Three pieces:

1. **`StockApp.sln` / `src/StockApp.Api`** — ASP.NET Core 8 Web API (C#). Owns users,
   risk profiles, notifications, trade history, and Alpaca order execution.
2. **`frontend/stockapp-web`** — React (Vite) frontend: risk questionnaire, watchlist
   analysis, notifications feed, trade history.
3. **`agent/`** — Python FastAPI "brain": a LangGraph pipeline (`llm_graph.py`)
   that runs a rule-based Shariah compliance gate first, then news sentiment
   (VADER over NewsAPI headlines), fundamentals (FMP) and momentum (yfinance),
   and asks a local LLM via Ollama (default `qwen2.5:7b`, JSON-constrained output)
   to return a buy/sell/hold signal with a confidence score, weighted by the
   user's risk appetite. Non-compliant stocks short-circuit to "hold" and never
   reach the LLM.

## How it fits together

```
React (Vite, :5173) → ASP.NET Core API (:7001) → Python Agent (FastAPI, :8001)
                              │                        │
                              │                   NewsAPI / FMP / yfinance
                              ▼
                        Alpaca (paper trading)
```

The .NET backend polls the agent every 15 minutes (`AgentPollingBackgroundService`)
for each user's watchlist, or you can trigger it on demand from the UI
("Analyze Now"). For every signal:
- If Shariah-non-compliant → discarded, never surfaced or traded.
- If the user has **auto-trade OFF** → a notification/alert is created; the user
  decides manually.
- If **auto-trade ON** → the backend sizes the position per the user's risk
  profile (max % per position, etc.) and places a market order via Alpaca.

## Free API keys you'll need

| Service | Used for | Free tier |
|---|---|---|
| [Alpaca](https://alpaca.markets) | Paper trading account, price data, order execution | Yes — unlimited paper trading |
| [NewsAPI](https://newsapi.org) | Recent headlines per company for sentiment | Yes — 100 requests/day (dev) |
| [Financial Modeling Prep](https://financialmodelingprep.com) | Balance sheet & ratios for Shariah screen + fundamentals score | Yes — limited requests/day |
| Yahoo Finance (via `yfinance`) | Price history / momentum | No key needed |

## Running it

### 1. Python agent
Requires [Ollama](https://ollama.com) running locally with the model pulled
(`ollama pull qwen2.5:7b`, or set `OLLAMA_MODEL` in `.env`).
```bash
cd agent
python -m venv venv && source venv/bin/activate   # Windows: venv\Scripts\activate
pip install -r requirements.txt
cp .env.example .env   # fill in NEWSAPI_KEY, FMP_API_KEY, ALPACA_* keys
uvicorn main:app --reload --port 8001
```

### 2. .NET backend
Open `StockApp.sln` in Visual Studio (Enterprise or Community — both work), or:
```bash
cd src/StockApp.Api
dotnet restore
dotnet run
```
Fill in your Alpaca keys in `appsettings.json` (`Alpaca:ApiKeyId` / `Alpaca:ApiSecretKey`).
Note the port it launches on (check `Properties/launchSettings.json` or console
output) and update `BASE_URL` in `frontend/stockapp-web/src/api.js` to match.

### 3. React frontend
```bash
cd frontend/stockapp-web
npm install
npm run dev
```
Open http://localhost:5173

## Important notes

- **This is a scaffold, not a finished product.** The Shariah screen is a
  rule-based approximation of the common AAOIFI-style methodology (debt,
  cash/securities, and non-permissible income all under ~33%/5% of market cap)
  — it is **not** a fatwa and should be cross-checked against a dedicated
  screening provider (e.g. Zoya, Musaffa, IdealRatings) before trusting it with
  real money.
- Ships pointed at **Alpaca's paper trading endpoint** by default
  (`https://paper-api.alpaca.markets`). Switch to the live endpoint only once
  you've tested thoroughly and understand the risk — this bot places real
  market orders when auto-trade is on.
- The "sell" sizing logic in `TradeExecutionService.cs` is a placeholder
  (sells 1 share) — hook up Alpaca's `/v2/positions` endpoint to sell your
  actual held quantity before using this for real.
- Auth is minimal: register/login with BCrypt-hashed passwords, but no JWT or
  session tokens are issued, so the API trusts the `userId` in the URL — add
  token-based auth before deploying this anywhere multi-user.

## Status

- `dotnet build` of `src/StockApp.Api` succeeds (0 warnings, 0 errors, checked 2026-11-21).
- The Python agent and React frontend have not been run end-to-end in that check;
  they need the API keys above and a running Ollama instance.
- Defaults to the Alpaca paper-trading endpoint.
