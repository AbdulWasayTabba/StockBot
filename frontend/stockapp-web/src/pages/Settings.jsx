import { useState } from 'react';
import { useAuth } from '../context/AuthContext.jsx';
import RiskProfileForm from '../components/RiskProfileForm.jsx';

export default function Settings() {
  const { user } = useAuth();
  const [error, setError] = useState(null);

  return (
    <>
      <h1 className="page-title">Settings</h1>
      {error && <div className="error-banner">⚠ {error}</div>}
      <RiskProfileForm userId={user.id} onError={setError} />
    </>
  );
}
