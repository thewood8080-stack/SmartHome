// פריסת האפליקציה — sidebar במחשב, bottom nav בנייד
import { useState } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { STRINGS } from '../utils/strings';

const navItems = [
  { to: '/dashboard',   icon: '🏠', label: STRINGS.nav.dashboard },
  { to: '/tasks',       icon: '✅', label: STRINGS.nav.tasks },
  { to: '/shopping',    icon: '🛒', label: STRINGS.nav.shopping },
  { to: '/budget',      icon: '💰', label: STRINGS.nav.budget },
  { to: '/calendar',    icon: '📅', label: STRINGS.nav.calendar },
  { to: '/gifts',       icon: '🎁', label: STRINGS.nav.gifts },
  { to: '/inventory',   icon: '📦', label: STRINGS.nav.inventory },
  { to: '/medical',     icon: '🏥', label: STRINGS.nav.medical },
  { to: '/vehicle',     icon: '🚗', label: STRINGS.nav.vehicle },
  { to: '/leaderboard', icon: '🏆', label: STRINGS.nav.gamification },
  { to: '/admin',       icon: '⚙️', label: 'ניהול', adminOnly: true },
  { to: '/help',        icon: '❓', label: 'מדריך' },
];

// 5 פריטים ראשיים לתפריט תחתון
const bottomNav = [
  { to: '/dashboard', icon: '🏠', label: 'בית' },
  { to: '/tasks',     icon: '✅', label: 'משימות' },
  { to: '/shopping',  icon: '🛒', label: 'קניות' },
  { to: '/calendar',  icon: '📅', label: 'יומן' },
  { to: '/more',      icon: '☰',  label: 'עוד' },
];

interface LayoutProps { children: React.ReactNode; }

export default function Layout({ children }: LayoutProps) {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();
  const [showMore, setShowMore] = useState(false);

  const handleLogout = () => { logout(); navigate('/login'); };

  const filteredNav = navItems.filter((i) => !i.adminOnly || user?.role === 'admin');

  return (
    <div style={{ display: 'flex', minHeight: '100vh' }}>

      {/* ===== SIDEBAR — מחשב בלבד ===== */}
      <nav style={styles.sidebar}>
        <div style={styles.logo}>
          <span style={{ fontSize: '1.4rem' }}>🏡</span>
          <span style={styles.logoText}>{STRINGS.app.name}</span>
        </div>

        <div style={{ flex: 1, overflowY: 'auto' }}>
          {filteredNav.map((item) => (
            <NavLink key={item.to} to={item.to}
              style={({ isActive }) => ({
                ...styles.navItem,
                background: isActive ? 'rgba(201,168,76,0.15)' : 'transparent',
                color: isActive ? 'var(--color-secondary)' : 'rgba(255,255,255,0.85)',
                fontWeight: isActive ? 700 : 400,
              })}
            >
              <span>{item.icon}</span><span>{item.label}</span>
            </NavLink>
          ))}
        </div>

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

      {/* ===== תוכן ראשי ===== */}
      <main style={styles.main}>
        {children}
      </main>

      {/* ===== BOTTOM NAV — נייד בלבד ===== */}
      <nav style={styles.bottomNav}>
        {bottomNav.map((item) =>
          item.to === '/more' ? (
            <button key="more" onClick={() => setShowMore(!showMore)} style={styles.bottomItem}>
              <span style={{ fontSize: '1.3rem' }}>{item.icon}</span>
              <span style={styles.bottomLabel}>{item.label}</span>
            </button>
          ) : (
            <NavLink key={item.to} to={item.to} style={({ isActive }) => ({
              ...styles.bottomItem,
              color: isActive ? 'var(--color-secondary)' : 'rgba(255,255,255,0.7)',
            })}>
              <span style={{ fontSize: '1.3rem' }}>{item.icon}</span>
              <span style={styles.bottomLabel}>{item.label}</span>
            </NavLink>
          )
        )}
      </nav>

      {/* ===== תפריט "עוד" ===== */}
      {showMore && (
        <>
          <div onClick={() => setShowMore(false)} style={styles.overlay} />
          <div style={styles.moreMenu}>
            <div style={styles.moreHeader}>
              <div style={styles.userInfoMobile}>
                <div style={styles.avatarFallback}>{user?.name?.[0]}</div>
                <div>
                  <div style={{ fontWeight: 700, color: '#fff' }}>{user?.name}</div>
                  <div style={{ fontSize: '0.78rem', color: 'rgba(255,255,255,0.6)' }}>
                    {user?.role === 'admin' ? '👑 מנהל' : '👤 משתמש'} · ⭐ {user?.points || 0}
                  </div>
                </div>
              </div>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem', padding: '1rem' }}>
              {filteredNav.slice(4).map((item) => (
                <NavLink key={item.to} to={item.to}
                  onClick={() => setShowMore(false)}
                  style={({ isActive }) => ({
                    ...styles.moreItem,
                    background: isActive ? 'rgba(201,168,76,0.2)' : 'rgba(255,255,255,0.05)',
                    color: isActive ? 'var(--color-secondary)' : '#fff',
                  })}
                >
                  <span style={{ fontSize: '1.5rem' }}>{item.icon}</span>
                  <span style={{ fontSize: '0.82rem' }}>{item.label}</span>
                </NavLink>
              ))}
            </div>
            <button onClick={handleLogout} style={styles.logoutBtnMobile}>🚪 יציאה</button>
          </div>
        </>
      )}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  sidebar: {
    width: '220px',
    minHeight: '100vh',
    background: 'var(--color-primary)',
    display: 'flex' as const,
    flexDirection: 'column' as const,
    padding: '1rem 0',
    position: 'sticky' as const,
    top: 0,
    flexShrink: 0,
    // מוסתר במובייל דרך CSS
  },
  logoText: { color: 'var(--color-secondary)', fontWeight: 700, fontSize: '1.1rem' },
  logo: { display: 'flex', alignItems: 'center', gap: '0.5rem', padding: '0.5rem 1rem 1.25rem', borderBottom: '1px solid rgba(255,255,255,0.1)', marginBottom: '0.5rem' },
  navItem: { display: 'flex', alignItems: 'center', gap: '0.6rem', padding: '0.65rem 1rem', borderRadius: '8px', margin: '2px 0.5rem', fontSize: '0.9rem', transition: 'background 0.15s' },
  userSection: { padding: '1rem', borderTop: '1px solid rgba(255,255,255,0.1)', marginTop: '0.5rem' },
  userInfo: { display: 'flex', alignItems: 'center', gap: '0.6rem', marginBottom: '0.75rem' },
  avatar: { width: '34px', height: '34px', borderRadius: '50%', objectFit: 'cover' as const },
  avatarFallback: { width: '36px', height: '36px', borderRadius: '50%', background: 'var(--color-secondary)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: '1rem', flexShrink: 0 },
  logoutBtn: { width: '100%', padding: '0.5rem', background: 'rgba(231,76,60,0.2)', color: '#ff8a80', borderRadius: '8px', fontSize: '0.85rem', fontWeight: 600 },
  main: { flex: 1, padding: '1.25rem', overflowY: 'auto' as const, paddingBottom: '5rem' },
  // bottom nav
  bottomNav: {
    display: 'none',
    position: 'fixed' as const,
    bottom: 0, left: 0, right: 0,
    background: 'var(--color-primary)',
    borderTop: '1px solid rgba(255,255,255,0.1)',
    zIndex: 1000,
    height: '60px',
  },
  bottomItem: { flex: 1, display: 'flex', flexDirection: 'column' as const, alignItems: 'center', justifyContent: 'center', gap: '2px', background: 'none', color: 'rgba(255,255,255,0.7)', textDecoration: 'none', padding: '0.25rem 0' },
  bottomLabel: { fontSize: '0.65rem', fontWeight: 600 },
  // תפריט עוד
  overlay: { position: 'fixed' as const, inset: 0, background: 'rgba(0,0,0,0.5)', zIndex: 1001 },
  moreMenu: { position: 'fixed' as const, bottom: '60px', left: 0, right: 0, background: 'var(--color-primary)', borderRadius: '16px 16px 0 0', zIndex: 1002, maxHeight: '70vh', overflowY: 'auto' as const },
  moreHeader: { padding: '1rem', borderBottom: '1px solid rgba(255,255,255,0.1)' },
  userInfoMobile: { display: 'flex', alignItems: 'center', gap: '0.75rem' },
  moreItem: { display: 'flex', flexDirection: 'column' as const, alignItems: 'center', gap: '0.3rem', padding: '0.85rem 0.5rem', borderRadius: '12px', textDecoration: 'none' },
  logoutBtnMobile: { width: 'calc(100% - 2rem)', margin: '0 1rem 1rem', padding: '0.75rem', background: 'rgba(231,76,60,0.2)', color: '#ff8a80', borderRadius: '10px', fontWeight: 700, fontSize: '0.95rem' },
};
