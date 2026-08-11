import { useState } from 'react';
import { useAuth } from '../context/AuthContext.jsx';
import Notifications from '../components/Notifications.jsx';

export default function NotificationsPage() {
  const { user } = useAuth();
  const [error, setError] = useState(null);

  return (
    <>
      <h1 className="page-title">Notifications</h1>
      {error && <div className="error-banner">⚠ {error}</div>}
      <Notifications userId={user.id} onError={setError} />
    </>
  );
}
