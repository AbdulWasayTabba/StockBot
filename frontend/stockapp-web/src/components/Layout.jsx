import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';
import PriceTicker from './PriceTicker.jsx';

export default function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="logo">📈 StockApp</div>
        <NavLink to="/" end className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
          Dashboard
        </NavLink>
        <NavLink to="/explore" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
          Explore
        </NavLink>
        <NavLink to="/portfolio" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
          Portfolio
        </NavLink>
        <NavLink to="/notifications" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
          Notifications
        </NavLink>
        <NavLink to="/settings" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
          Settings
        </NavLink>

        <div style={{ flex: 1 }} />
        <div className="sidebar-user">
          <div className="sidebar-user-name">{user?.displayName}</div>
          <div className="sidebar-user-email">{user?.email}</div>
          <button className="secondary" style={{ width: '100%', marginTop: 10 }} onClick={handleLogout}>
            Log Out
          </button>
        </div>
      </aside>

      <div className="main-column">
        <PriceTicker />
        <main className="main">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
