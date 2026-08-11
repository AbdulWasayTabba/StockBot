import { useEffect, useState } from 'react';
import { api } from '../api.js';

export default function Notifications({ userId, onError }) {
  const [items, setItems] = useState([]);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    api.getNotifications(userId)
      .then((data) => { setItems(data); setLoaded(true); })
      .catch((e) => onError?.(`Failed to load notifications: ${e.message}`));
  }, [userId]);

  return (
    <div className="card">
      <h2>Notifications</h2>
      {!loaded && <div className="empty-state">Loading…</div>}
      {loaded && items.length === 0 && (
        <div className="empty-state">No alerts yet — run an analysis above to generate signals.</div>
      )}
      <div className="row-list">
        {items.map((n) => (
          <div className="symbol-row" key={n.id} style={{ opacity: n.isRead ? 0.5 : 1 }}>
            <div>
              <div className="ticker">{n.symbol}</div>
              <div className="meta">{n.message}</div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
