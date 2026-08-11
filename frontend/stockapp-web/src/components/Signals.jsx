import { useState } from 'react';
import { api } from '../api.js';

export default function Signals({ userId, onError }) {
  const [symbols, setSymbols] = useState('AAPL, MSFT, GOOGL');
  const [signals, setSignals] = useState([]);
  const [loading, setLoading] = useState(false);
  const [ran, setRan] = useState(false);

  const run = async () => {
    setLoading(true);
    try {
      const list = symbols.split(',').map((s) => s.trim().toUpperCase()).filter(Boolean);
      if (list.length === 0) {
        onError?.('Enter at least one ticker symbol.');
        return;
      }
      const result = await api.runSignals(userId, list);
      setSignals(result);
      setRan(true);
    } catch (e) {
      onError?.(
        `Analysis failed: ${e.message}. Make sure the Python agent is running on port 8001.`
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="card">
      <h2>Run Analysis</h2>
      <label>Watchlist (comma-separated tickers)</label>
      <input value={symbols} onChange={(e) => setSymbols(e.target.value)} />
      <div style={{ marginTop: 14 }}>
        <button onClick={run} disabled={loading}>
          {loading && <span className="spinner" />}
          {loading ? 'Analyzing…' : 'Analyze Now'}
        </button>
      </div>

      {ran && signals.length === 0 && (
        <div className="empty-state">No signals returned — check the agent terminal for errors.</div>
      )}

      {signals.length > 0 && (
        <div className="row-list" style={{ marginTop: 16 }}>
          {signals.map((s) => (
            <div className="symbol-row" key={s.symbol}>
              <div>
                <div className="ticker">
                  {s.symbol} {!s.shariah_compliant && <span title="Not Shariah-compliant">🚫</span>}
                </div>
                <div className="meta">{s.rationale}</div>
              </div>
              <div className="right">
                <span className={`pill ${s.action}`}>{s.action}</span>
                <div className="confidence-bar">
                  <div className="fill" style={{ width: `${Math.round(s.confidence * 100)}%` }} />
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
