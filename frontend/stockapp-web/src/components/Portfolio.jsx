import { useEffect, useState } from 'react';
import { api } from '../api.js';

export default function Portfolio({ userId, onError }) {
  const [trades, setTrades] = useState([]);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    api.getTrades(userId)
      .then((data) => { setTrades(data); setLoaded(true); })
      .catch((e) => onError?.(`Failed to load trade history: ${e.message}`));
  }, [userId]);

  return (
    <div className="card">
      <h2>Trade History</h2>
      {!loaded && <div className="empty-state">Loading…</div>}
      {loaded && trades.length === 0 && (
        <div className="empty-state">No trades yet.</div>
      )}
      <div className="row-list">
        {trades.map((t) => (
          <div className="symbol-row" key={t.id}>
            <div>
              <div className="ticker">{t.symbol}</div>
              <div className="meta">Qty {t.quantity} @ {t.price ?? 'market'} — {t.status}</div>
            </div>
            <div className="right">
              <span className={`pill ${t.side === 0 ? 'buy' : 'sell'}`}>
                {t.side === 0 ? 'buy' : 'sell'}
              </span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
