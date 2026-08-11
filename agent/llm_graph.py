"""
LangGraph pipeline for trade decisions.

Flow:
  shariah_gate -> (if compliant) -> [news_analyst, fundamentals_analyst] in parallel
                -> risk_manager (LLM synthesizes both + momentum, applies risk appetite)
                -> END

If shariah_gate fails, the graph short-circuits straight to a "hold, non-compliant"
result — the LLM never even sees non-compliant stocks, so it can't be talked into
recommending one.
"""

import json
from typing import TypedDict, Optional
from langgraph.graph import StateGraph, END
from langchain_ollama import ChatOllama

from config import OLLAMA_MODEL, OLLAMA_BASE_URL, RISK_PARAMS
from shariah_screen import screen_stock
from news_analysis import get_news_sentiment, get_fundamentals_snapshot
from market_data import get_momentum_score

llm = ChatOllama(
    model=OLLAMA_MODEL,
    base_url=OLLAMA_BASE_URL,
    temperature=0,
    format="json",  # forces Ollama to constrain output to valid JSON
)


class AgentState(TypedDict, total=False):
    symbol: str
    risk_appetite: str
    shariah_compliant: bool
    shariah_notes: str
    news: dict
    fundamentals: dict
    momentum: dict
    action: str
    confidence: float
    rationale: str


async def shariah_gate(state: AgentState) -> AgentState:
    result = await screen_stock(state["symbol"])
    return {"shariah_compliant": result.compliant, "shariah_notes": result.notes}


def shariah_router(state: AgentState) -> str:
    return "analyze" if state["shariah_compliant"] else "reject"


async def dispatch_node(state: AgentState) -> AgentState:
    # LangGraph requires every node to write at least one key. Re-writing
    # shariah_compliant with its own unchanged value is a safe no-op here.
    return {"shariah_compliant": state["shariah_compliant"]}


async def reject_node(state: AgentState) -> AgentState:
    return {
        "action": "hold",
        "confidence": 0.0,
        "rationale": f"Not Shariah-compliant: {state['shariah_notes']}",
    }


async def news_analyst(state: AgentState) -> AgentState:
    news = await get_news_sentiment(state["symbol"])
    return {"news": news}


async def fundamentals_analyst(state: AgentState) -> AgentState:
    fundamentals = await get_fundamentals_snapshot(state["symbol"])
    momentum = await get_momentum_score(state["symbol"])
    return {"fundamentals": fundamentals, "momentum": momentum}


RISK_MANAGER_PROMPT = """You are a cautious risk manager deciding whether to BUY, SELL, \
or HOLD a stock on behalf of a client, for a REAL brokerage account with real money.

Stock: {symbol}
Client risk appetite: {risk_appetite}

News sentiment score (-1 to 1): {news_score}
Recent headlines: {headlines}

Fundamentals score (-1 to 1): {fundamentals_score}
Fundamentals detail: {fundamentals_detail}

Momentum score (-1 to 1): {momentum_score}
Momentum detail: {momentum_detail}

Risk profile confidence threshold required to act: {threshold}

Rules:
- Be conservative. When evidence is mixed, weak, or stale, choose HOLD.
- A more aggressive risk appetite tolerates lower confidence and more momentum-driven
  trades; a conservative appetite requires strong fundamentals and should rarely trade
  on news/momentum alone.
- Never recommend BUY or SELL below the stated confidence threshold.
- Confidence must reflect genuine conviction, not just "some signal exists."

Respond with ONLY valid JSON, no markdown fences, no preamble:
{{"action": "buy" | "sell" | "hold", "confidence": <0.0-1.0>, "rationale": "<2-3 sentences, plain language, cite the specific evidence you weighted>"}}
"""


async def risk_manager(state: AgentState) -> AgentState:
    params = RISK_PARAMS.get(state["risk_appetite"], RISK_PARAMS["Moderate"])
    news = state.get("news", {"score": 0.0, "headlines": [], "detail": "n/a"})
    fundamentals = state.get("fundamentals", {"score": 0.0, "detail": "n/a"})
    momentum = state.get("momentum", {"score": 0.0, "detail": "n/a"})

    prompt = RISK_MANAGER_PROMPT.format(
        symbol=state["symbol"],
        risk_appetite=state["risk_appetite"],
        news_score=news["score"],
        headlines="; ".join(news.get("headlines", [])) or "none found",
        fundamentals_score=fundamentals["score"],
        fundamentals_detail=fundamentals["detail"],
        momentum_score=momentum["score"],
        momentum_detail=momentum["detail"],
        threshold=params["confidence_threshold"],
    )

    try:
        response = await llm.ainvoke(prompt)
        text = response.content.strip()
        # strip accidental markdown fences if the model adds them
        if text.startswith("```"):
            text = text.strip("`").removeprefix("json").strip()
        parsed = json.loads(text)
        action = parsed.get("action", "hold")
        confidence = float(parsed.get("confidence", 0.0))
        rationale = parsed.get("rationale", "No rationale provided.")
    except Exception as e:
        action, confidence, rationale = "hold", 0.0, f"Risk manager LLM call failed, defaulting to HOLD: {e}"

    # Hard backstop: even if the LLM is overconfident, never act below the
    # risk profile's required threshold.
    if confidence < params["confidence_threshold"]:
        action = "hold"

    return {"action": action, "confidence": confidence, "rationale": rationale}


def build_graph():
    graph = StateGraph(AgentState)
    graph.add_node("shariah_gate", shariah_gate)
    graph.add_node("dispatch", dispatch_node)
    graph.add_node("news_analyst", news_analyst)
    graph.add_node("fundamentals_analyst", fundamentals_analyst)
    graph.add_node("risk_manager", risk_manager)
    graph.add_node("reject", reject_node)

    graph.set_entry_point("shariah_gate")
    graph.add_conditional_edges(
        "shariah_gate", shariah_router, {"analyze": "dispatch", "reject": "reject"}
    )
    # fan out: both analysts run in parallel off dispatch, both feed risk_manager
    graph.add_edge("dispatch", "news_analyst")
    graph.add_edge("dispatch", "fundamentals_analyst")
    graph.add_edge("news_analyst", "risk_manager")
    graph.add_edge("fundamentals_analyst", "risk_manager")
    graph.add_edge("risk_manager", END)
    graph.add_edge("reject", END)

    return graph.compile()


_compiled_graph = None


async def analyze_symbol_graph(symbol: str, risk_appetite: str) -> dict:
    global _compiled_graph
    if _compiled_graph is None:
        _compiled_graph = build_graph()

    result = await _compiled_graph.ainvoke({"symbol": symbol, "risk_appetite": risk_appetite})
    return {
        "symbol": symbol,
        "action": result.get("action", "hold"),
        "confidence": result.get("confidence", 0.0),
        "rationale": result.get("rationale", ""),
        "shariah_compliant": result.get("shariah_compliant", False),
    }
