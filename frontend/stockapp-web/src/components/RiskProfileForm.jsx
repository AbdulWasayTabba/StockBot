import { useState } from 'react';
import { api } from '../api.js';

export default function RiskProfileForm({ userId, onError }) {
  const [appetite, setAppetite] = useState('Moderate');
  const [maxPosition, setMaxPosition] = useState(10);
  const [stopLoss, setStopLoss] = useState(8);
  const [takeProfit, setTakeProfit] = useState(20);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  const save = async () => {
    setSaving(true);
    setSaved(false);
    try {
      await api.saveRiskProfile(userId, {
        appetite: ['Conservative', 'Moderate', 'Aggressive'].indexOf(appetite),
        maxPositionSizePercent: Number(maxPosition),
        maxDailyTurnoverPercent: 20,
        stopLossPercent: Number(stopLoss),
        takeProfitPercent: Number(takeProfit),
        shariahComplianceOnly: true,
      });
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    } catch (e) {
      onError?.(`Failed to save risk profile: ${e.message}`);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="card">
      <h2>Risk Appetite</h2>
      <label>Risk level</label>
      <select value={appetite} onChange={(e) => setAppetite(e.target.value)}>
        <option>Conservative</option>
        <option>Moderate</option>
        <option>Aggressive</option>
      </select>

      <label>Max position size (% of portfolio per stock)</label>
      <input type="number" value={maxPosition} onChange={(e) => setMaxPosition(e.target.value)} />

      <label>Stop loss (%)</label>
      <input type="number" value={stopLoss} onChange={(e) => setStopLoss(e.target.value)} />

      <label>Take profit (%)</label>
      <input type="number" value={takeProfit} onChange={(e) => setTakeProfit(e.target.value)} />

      <div style={{ marginTop: 16, display: 'flex', alignItems: 'center', gap: 12 }}>
        <button onClick={save} disabled={saving}>
          {saving && <span className="spinner" />}
          {saving ? 'Saving…' : 'Save Risk Profile'}
        </button>
        {saved && <span style={{ color: 'var(--green)', fontSize: 13, fontWeight: 600 }}>Saved ✓</span>}
      </div>
    </div>
  );
}
