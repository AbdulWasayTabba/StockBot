import { useState } from 'react';
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts';
import { api } from '../api.js';

export default function Explore() {
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [result, setResult] = useState(null); // { symbol, price, compliance, history }

  const search = async (e) => {
    e?.preventDefault();
    const symbol = query.trim().toUpperCase();
    if (!symbol) return;

    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const [priceRes, complianceRes, historyRes] = await Promise.all([
        api.getPrice(symbol).catch(() => null),
        api.getCompliance(symbol).catch(() => null),
        api.getHistory(symbol, 90).catch(() => []),
      ]);

      if (!priceRes && (!historyRes || historyRes.length === 0)) {
        setError(`No data found for "${symbol}" — check the ticker is correct and is US-listed (Alpaca only covers US exchanges).`);
        return;
      }

      const chartData = (historyRes || []).map((b) => ({
        date: new Date(b.time).toLocaleDateString(undefined, { month: 'short', day: 'numeric' }),
        close: b.close,
      }));

      setResult({
        symbol,
        price: priceRes?.price ?? chartData[chartData.length - 1]?.close ?? null,
        compliance: complianceRes,
        chartData,
      });
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <h1 className="page-title">Explore</h1>

      <form className="card" onSubmit={search} style={{ display: 'flex', gap: 10, alignItems: 'flex-end' }}>
        <div style={{ flex: 1 }}>
          <label style={{ marginTop: 0 }}>Search a US stock ticker</label>
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="e.g. AAPL, MSFT, TSLA"
            style={{ textTransform: 'uppercase' }}
          />
        </div>
        <button type="submit" disabled={loading}>
          {loading ? 'Searching…' : 'Search'}
        </button>
      </form>

      {error && <div className="error-banner">⚠ {error}</div>}

      {result && (
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 16 }}>
            <div>
              <div style={{ fontSize: 24, fontWeight: 700 }}>{result.symbol}</div>
              <div style={{ fontSize: 32, fontWeight: 700, marginTop: 4 }}>
                {result.price != null ? `$${Number(result.price).toFixed(2)}` : '—'}
              </div>
            </div>
            {result.compliance && (
              <div style={{ textAlign: 'right' }}>
                <span className={`pill ${result.compliance.compliant ? 'buy' : 'sell'}`}>
                  {result.compliance.compliant ? '✅ Shariah Compliant' : '🚫 Not Compliant'}
                </span>
                <div className="meta" style={{ marginTop: 6, maxWidth: 280 }}>{result.compliance.notes}</div>
              </div>
            )}
          </div>

          {result.chartData.length > 0 ? (
            <ResponsiveContainer width="100%" height={280}>
              <LineChart data={result.chartData}>
                <CartesianGrid strokeDasharray="3 3" stroke="#26272b" />
                <XAxis dataKey="date" stroke="#8e8e93" fontSize={12} />
                <YAxis stroke="#8e8e93" fontSize={12} domain={['auto', 'auto']} />
                <Tooltip
                  contentStyle={{ background: '#16171a', border: '1px solid #26272b', borderRadius: 8 }}
                  labelStyle={{ color: '#8e8e93' }}
                />
                <Line type="monotone" dataKey="close" stroke="#6e56cf" strokeWidth={2} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          ) : (
            <div className="empty-state">No chart history available for this symbol.</div>
          )}
        </div>
      )}
    </>
  );
}
