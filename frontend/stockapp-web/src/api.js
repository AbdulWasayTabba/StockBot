const BASE_URL = 'http://localhost:5000/api'; // matches `dotnet run` default (check your console output)

async function request(path, options = {}) {
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(text || `API error ${res.status}`);
  }
  return res.status === 204 ? null : res.json();
}

export const api = {
  register: (email, password, displayName) =>
    request('/auth/register', { method: 'POST', body: JSON.stringify({ email, password, displayName }) }),
  login: (email, password) =>
    request('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),

  getUser: (id) => request(`/users/${id}`),
  setAutoTrade: (id, enabled) =>
    request(`/users/${id}/auto-trade`, { method: 'PUT', body: JSON.stringify(enabled) }),

  getRiskProfile: (userId) => request(`/users/${userId}/risk-profile`),
  saveRiskProfile: (userId, profile) =>
    request(`/users/${userId}/risk-profile`, { method: 'POST', body: JSON.stringify(profile) }),

  getTrades: (userId) => request(`/users/${userId}/portfolio/trades`),
  getNotifications: (userId) => request(`/users/${userId}/notifications`),
  markNotificationRead: (userId, id) =>
    request(`/users/${userId}/notifications/${id}/read`, { method: 'PUT' }),

  runSignals: (userId, symbols) =>
    request('/signals/run', { method: 'POST', body: JSON.stringify({ userId, symbols }) }),

  getPrice: (symbol) => request(`/marketdata/price/${symbol}`),
  getAccount: () => request(`/marketdata/account`),
  getHistory: (symbol, days = 90) => request(`/marketdata/history/${symbol}?days=${days}`),
  getCompliance: (symbol) => request(`/marketdata/compliance/${symbol}`),
};
