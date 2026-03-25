// דשבורד ראשי
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';
import { useAuthStore } from '../../store/authStore';

interface Stats {
  tasks: number;
  tasksDone: number;
  shopping: number;
  shoppingBought: number;
  events: number;
  inventory: number;
}

export default function DashboardPage() {
  const { user } = useAuthStore();
  const navigate = useNavigate();
  const [stats, setStats] = useState<Stats>({ tasks: 0, tasksDone: 0, shopping: 0, shoppingBought: 0, events: 0, inventory: 0 });
  const [leaderboard, setLeaderboard] = useState<{ _id: string; name: string; points: number; photoURL?: string }[]>([]);

  useEffect(() => {
    const load = async () => {
      try {
        const [tasks, shopping, events, inventory, users] = await Promise.all([
          api.get('/tasks'),
          api.get('/shopping'),
          api.get('/events'),
          api.get('/inventory'),
          api.get('/users/leaderboard'),
        ]);
        setStats({
          tasks: tasks.data.length,
          tasksDone: tasks.data.filter((t: { status: string }) => t.status === 'done').length,
          shopping: shopping.data.length,
          shoppingBought: shopping.data.filter((s: { bought: boolean }) => s.bought).length,
          events: events.data.length,
          inventory: inventory.data.length,
        });
        setLeaderboard(users.data.slice(0, 3));
      } catch { /* שגיאה שקטה */ }
    };
    load();
  }, []);

  const shortcuts = [
    { icon: '✅', label: 'משימות', to: '/tasks', color: '#1E3A5F' },
    { icon: '🛒', label: 'קניות', to: '/shopping', color: '#27AE60' },
    { icon: '📅', label: 'לוח שנה', to: '/calendar', color: '#8E44AD' },
    { icon: '🎁', label: 'מתנות', to: '/gifts', color: '#C9A84C' },
    { icon: '💰', label: 'תקציב', to: '/budget', color: '#2980B9' },
    { icon: '📦', label: 'מלאי', to: '/inventory', color: '#E67E22' },
    { icon: '🏥', label: 'רפואה', to: '/medical', color: '#E74C3C' },
    { icon: '🚗', label: 'רכב', to: '/vehicle', color: '#34495E' },
  ];

  return (
    <div>
      {/* כותרת */}
      <div style={styles.header}>
        <div>
          <h1 style={styles.greeting}>שלום, {user?.name?.split(' ')[0]} 👋</h1>
          <p style={styles.date}>{new Date().toLocaleDateString('he-IL', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</p>
        </div>
        <div style={styles.pointsBadge}>
          <span style={{ fontSize: '1.4rem' }}>⭐</span>
          <span style={{ fontWeight: 700, fontSize: '1.1rem' }}>{user?.points || 0}</span>
          <span style={{ fontSize: '0.8rem', color: 'rgba(255,255,255,0.8)' }}>נקודות</span>
        </div>
      </div>

      {/* סטטיסטיקות */}
      <div className="grid-3" style={{ marginBottom: '1.5rem' }}>
        <StatCard icon="✅" label="משימות שבוצעו" value={`${stats.tasksDone}/${stats.tasks}`} color="#1E3A5F" />
        <StatCard icon="🛒" label="קניות שנקנו" value={`${stats.shoppingBought}/${stats.shopping}`} color="#27AE60" />
        <StatCard icon="📅" label="אירועים קרובים" value={String(stats.events)} color="#8E44AD" />
      </div>

      {/* קיצורי דרך */}
      <h2 style={styles.sectionTitle}>מודולים</h2>
      <div style={styles.shortcutsGrid}>
        {shortcuts.map((s) => (
          <button key={s.to} onClick={() => navigate(s.to)} style={{ ...styles.shortcut, background: s.color }}>
            <span style={{ fontSize: '1.8rem' }}>{s.icon}</span>
            <span style={{ fontSize: '0.85rem', fontWeight: 600, color: '#fff' }}>{s.label}</span>
          </button>
        ))}
      </div>

      {/* לוח מובילים */}
      {leaderboard.length > 0 && (
        <>
          <h2 style={styles.sectionTitle}>🏆 מובילים השבוע</h2>
          <div className="card">
            {leaderboard.map((u, i) => (
              <div key={u._id} style={styles.leaderRow}>
                <span style={styles.rank}>{['🥇','🥈','🥉'][i]}</span>
                <span style={{ flex: 1, fontWeight: 600 }}>{u.name}</span>
                <span style={{ color: 'var(--color-secondary)', fontWeight: 700 }}>⭐ {u.points}</span>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

function StatCard({ icon, label, value, color }: { icon: string; label: string; value: string; color: string }) {
  return (
    <div className="card" style={{ borderTop: `4px solid ${color}`, textAlign: 'center' }}>
      <div style={{ fontSize: '1.8rem', marginBottom: '0.25rem' }}>{icon}</div>
      <div style={{ fontSize: '1.4rem', fontWeight: 700, color }}>{value}</div>
      <div style={{ fontSize: '0.82rem', color: 'var(--color-gray)' }}>{label}</div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    background: 'var(--color-primary)',
    borderRadius: '16px',
    padding: '1.25rem 1.5rem',
    marginBottom: '1.5rem',
    color: '#fff',
  },
  greeting: { fontSize: '1.4rem', fontWeight: 700 },
  date: { fontSize: '0.85rem', color: 'rgba(255,255,255,0.7)', marginTop: '0.2rem' },
  pointsBadge: {
    background: 'rgba(255,255,255,0.15)',
    borderRadius: '12px',
    padding: '0.6rem 1rem',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '0.1rem',
    color: '#fff',
  },
  sectionTitle: {
    fontSize: '1.1rem',
    fontWeight: 700,
    color: 'var(--color-primary)',
    marginBottom: '0.75rem',
  },
  shortcutsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(4, 1fr)',
    gap: '0.75rem',
    marginBottom: '1.5rem',
  },
  shortcut: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '0.4rem',
    padding: '1rem 0.5rem',
    borderRadius: '12px',
    border: 'none',
    cursor: 'pointer',
    transition: 'transform 0.15s',
  },
  leaderRow: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.75rem',
    padding: '0.6rem 0',
    borderBottom: '1px solid var(--color-light)',
  },
  rank: { fontSize: '1.3rem', width: '2rem' },
};
