import { useEffect, useState } from 'react';
import { api } from '../api.js';

const TICKER_SYMBOLS = ['AAPL', 'MSFT', 'GOOGL', 'AMZN', 'NVDA'];

// A horizontal scrolling price bar, similar to the PSX/Bloomberg-style
// ticker strip. Pulls live last-trade prices from the backend (which proxies
// Alpaca's market data API).
export default function PriceTicker() {
  const [prices, setPrices] = useState({});

  useEffect(() => {
    let cancelled = false;

    const fetchAll = async () => {
      const results = await Promise.all(
        TICKER_SYMBOLS.map((s) =>
          api.getPrice(s).then((r) => [s, r.price]).catch(() => [s, null])
        )
      );
      if (!cancelled) setPrices(Object.fromEntries(results));
    };

    fetchAll();
    const interval = setInterval(fetchAll, 30000); // refresh every 30s
    return () => { cancelled = true; clearInterval(interval); };
  }, []);

  return (
    <div className="ticker-bar">
      {TICKER_SYMBOLS.map((s) => (
        <div className="ticker-item" key={s}>
          <span className="ticker-symbol">{s}</span>
          <span className="ticker-price">
            {prices[s] != null ? `$${Number(prices[s]).toFixed(2)}` : '—'}
          </span>
        </div>
      ))}
    </div>
  );
}
