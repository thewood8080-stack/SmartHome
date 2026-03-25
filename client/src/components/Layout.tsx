// פריסת האפליקציה עם ניווט צד
import { NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { STRINGS } from '../utils/strings';

const navItems = [
  { to: '/dashboard',     icon: '🏠', label: STRINGS.nav.dashboard },
  { to: '/tasks',         icon: '✅', label: STRINGS.nav.tasks },
  { to: '/shopping',      icon: '🛒', label: STRINGS.nav.shopping },
  { to: '/budget',        icon: '💰', label: STRINGS.nav.budget },
  { to: '/calendar',      icon: '📅', label: STRINGS.nav.calendar },
  { to: '/gifts',         icon: '🎁', label: STRINGS.nav.gifts },
  { to: '/inventory',     icon: '📦', label: STRINGS.nav.inventory },
  { to: '/medical',       icon: '🏥', label: STRINGS.nav.medical },
  { to: '/vehicle',       icon: '🚗', label: STRINGS.nav.vehicle },
  { to: '/leaderboard',   icon: '🏆', label: STRINGS.nav.gamification },
  { to: '/admin',         icon: '⚙️', label: 'ניהול', adminOnly: true },
];

interface LayoutProps {
  children: React.ReactNode;
}

export default function Layout({ children }: LayoutProps) {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="layout">
      {/* סרגל ניווט צדדי */}
      <nav style={styles.sidebar}>
        <div style={styles.logo}>
          <span style={{ fontSize: '1.5rem' }}>🏡</span>
          <span style={styles.logoText}>{STRINGS.app.name}</span>
        </div>

        <div style={styles.navList}>
          {navItems.filter((item) => !item.adminOnly || user?.role === 'admin').map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              style={({ isActive }) => ({
                ...styles.navItem,
                background: isActive ? 'rgba(201,168,76,0.15)' : 'transparent',
                color: isActive ? 'var(--color-secondary)' : 'rgba(255,255,255,0.85)',
                fontWeight: isActive ? 700 : 400,
              })}
            >
              <span>{item.icon}</span>
              <span>{item.label}</span>
            </NavLink>
          ))}
        </div>

        {/* פרופיל משתמש */}
        <div style={styles.userSection}>
          <div style={styles.userInfo}>
            {user?.photoURL
              ? <img src={user.photoURL} alt={user.name} style={styles.avatar} />
              : <div style={styles.avatarFallback}>{user?.name?.[0]}</div>
            }
            <div>
              <div style={{ fontSize: '0.85rem', fontWeight: 600, color: '#fff' }}>{user?.name}</div>
              <div style={{ fontSize: '0.75rem', color: 'rgba(255,255,255,0.6)' }}>
                {user?.role === 'admin' ? '👑 מנהל' : '👤 משתמש'}
              </div>
            </div>
          </div>
          <button onClick={handleLogout} style={styles.logoutBtn}>יציאה</button>
        </div>
      </nav>

      {/* תוכן ראשי */}
      <main className="main-content">
        {children}
      </main>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  sidebar: {
    width: '220px',
    minHeight: '100vh',
    background: 'var(--color-primary)',
    display: 'flex',
    flexDirection: 'column',
    padding: '1rem 0',
    position: 'sticky',
    top: 0,
    flexShrink: 0,
  },
  logo: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.5rem',
    padding: '0.5rem 1rem 1.25rem',
    borderBottom: '1px solid rgba(255,255,255,0.1)',
    marginBottom: '0.5rem',
  },
  logoText: {
    color: 'var(--color-secondary)',
    fontWeight: 700,
    fontSize: '1.1rem',
  },
  navList: {
    flex: 1,
    overflowY: 'auto',
  },
  navItem: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.6rem',
    padding: '0.65rem 1rem',
    borderRadius: '8px',
    margin: '2px 0.5rem',
    fontSize: '0.9rem',
    transition: 'background 0.15s',
  },
  userSection: {
    padding: '1rem',
    borderTop: '1px solid rgba(255,255,255,0.1)',
    marginTop: '0.5rem',
  },
  userInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.6rem',
    marginBottom: '0.75rem',
  },
  avatar: {
    width: '34px',
    height: '34px',
    borderRadius: '50%',
    objectFit: 'cover',
  },
  avatarFallback: {
    width: '34px',
    height: '34px',
    borderRadius: '50%',
    background: 'var(--color-secondary)',
    color: '#fff',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontWeight: 700,
    fontSize: '1rem',
  },
  logoutBtn: {
    width: '100%',
    padding: '0.5rem',
    background: 'rgba(231,76,60,0.2)',
    color: '#ff8a80',
    borderRadius: '8px',
    fontSize: '0.85rem',
    fontWeight: 600,
  },
};
