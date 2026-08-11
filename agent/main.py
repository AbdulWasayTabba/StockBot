"""
Python agent service. Run with:
    uvicorn main:app --reload --port 8001

Exposes /analyze which the .NET backend calls (see StockApp.Api/Services/AgentClient.cs).
The agent itself contains no trade execution or persistence — the .NET backend
owns users, risk profiles, notifications, and calling Alpaca to place orders,
so the agent stays a stateless "brain".
"""

import asyncio
from fastapi import FastAPI
from pydantic import BaseModel

from llm_graph import analyze_symbol_graph

app = FastAPI(title="StockApp Agent", version="1.0")


class AnalyzeRequest(BaseModel):
    risk_appetite: str  # "Conservative" | "Moderate" | "Aggressive"
    symbols: list[str]


class SignalResponse(BaseModel):
    symbol: str
    action: str
    confidence: float
    rationale: str
    shariah_compliant: bool


@app.post("/analyze", response_model=list[SignalResponse])
async def analyze(req: AnalyzeRequest):
    results = await asyncio.gather(
        *[analyze_symbol_graph(sym, req.risk_appetite) for sym in req.symbols]
    )
    return [SignalResponse(**r) for r in results]


@app.get("/shariah/{symbol}")
async def shariah_status(symbol: str):
    from shariah_screen import screen_stock
    result = await screen_stock(symbol.upper())
    return {"symbol": result.symbol, "compliant": result.compliant, "notes": result.notes}


@app.get("/health")
async def health():
    return {"status": "ok"}
