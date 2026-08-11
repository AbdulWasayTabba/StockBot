import { useState } from 'react';
import { useAuth } from '../context/AuthContext.jsx';
import Portfolio from '../components/Portfolio.jsx';

export default function PortfolioPage() {
  const { user } = useAuth();
  const [error, setError] = useState(null);

  return (
    <>
      <h1 className="page-title">Portfolio</h1>
      {error && <div className="error-banner">⚠ {error}</div>}
      <Portfolio userId={user.id} onError={setError} />
    </>
  );
}
