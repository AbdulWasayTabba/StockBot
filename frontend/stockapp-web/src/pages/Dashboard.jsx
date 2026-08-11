import { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext.jsx';
import Signals from '../components/Signals.jsx';
import { api } from '../api.js';

export default function Dashboard() {
  const { user, refreshUser } = useAuth();
  const [error, setError] = useState(null);
  const [autoTrade, setAutoTrade] = useState(user?.autoTradeEnabled ?? false);
  const [saving, setSaving] = useState(false);
  const [account, setAccount] = useState(null);

  useEffect(() => {
    setAutoTrade(user?.autoTradeEnabled ?? false);
  }, [user?.autoTradeEnabled]);

  useEffect(() => {
    const loadAccount = () => api.getAccount().then(setAccount).catch(() => {});
    loadAccount();
    const interval = setInterval(loadAccount, 30000); // refresh every 30s
    return () => clearInterval(interval);
  }, []);

  const toggleAutoTrade = async (checked) => {
    setAutoTrade(checked);
    setSaving(true);
    try {
      await api.setAutoTrade(user.id, checked);
      await refreshUser();
    } catch (e) {
      setError(`Failed to update auto-trade setting: ${e.message}`);
      setAutoTrade(!checked);
    } finally {
      setSaving(false);
    }
  };

  const equity = account ? Number(account.equity) : null;
  const lastEquity = account ? Number(account.lastEquity) : null;
  const change = equity != null && lastEquity != null ? equity - lastEquity : null;
  const changePct = change != null && lastEquity ? (change / lastEquity) * 100 : null;

  return (
    <>
      <div className="portfolio-hero">
        <div className="label">Paper Trading Portfolio</div>
        <div className="value">
          {equity != null
            ? `$${equity.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
            : 'Loading…'}
        </div>
        {change != null && (
          <div className={`change ${change >= 0 ? 'positive' : 'negative'}`}>
            {change >= 0 ? '+' : ''}${change.toFixed(2)} ({changePct.toFixed(2)}%) today
          </div>
        )}
      </div>

      {error && <div className="error-banner">⚠ {error}</div>}

      <div className="card">
        <h2>Trading Mode</h2>
        <div className="toggle-row">
          <div>
            <div className="toggle-label">Auto-execute trades</div>
            <div className="toggle-sub">
              {autoTrade
                ? 'The bot will place BUY/SELL orders automatically via Alpaca (market hours only).'
                : "You'll get alerts and decide manually — nothing trades without you."}
            </div>
          </div>
          <label className="switch">
            <input type="checkbox" checked={autoTrade} disabled={saving} onChange={(e) => toggleAutoTrade(e.target.checked)} />
            <span className="slider" />
          </label>
        </div>
      </div>

      <Signals userId={user.id} onError={setError} />
    </>
  );
}
