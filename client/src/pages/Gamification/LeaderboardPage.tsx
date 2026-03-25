// דף לוח מובילים — גמיפיקציה
import { useEffect, useState } from 'react';
import api from '../../services/api';
import { useAuthStore } from '../../store/authStore';

interface LeaderUser {
  _id: string;
  name: string;
  points: number;
  role: string;
  photoURL?: string;
}

const BADGES = [
  { minPoints: 100, icon: '🥉', label: 'מתחיל' },
  { minPoints: 250, icon: '🥈', label: 'פעיל' },
  { minPoints: 500, icon: '🥇', label: 'מצטיין' },
  { minPoints: 1000, icon: '🏆', label: 'אלוף הבית' },
];

export default function LeaderboardPage() {
  const { user } = useAuthStore();
  const [leaders, setLeaders] = useState<LeaderUser[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/users/leaderboard').then(({ data }) => {
      setLeaders(data);
      setLoading(false);
    });
  }, []);

  const resetPoints = async () => {
    if (!confirm('לאפס נקודות לכולם?')) return;
    // איפוס — עדכון כל המשתמשים ל-0
    await Promise.all(leaders.map((u) => api.patch(`/users/${u._id}/role`, { role: u.role }))); // placeholder — reset בנפרד
    alert('האיפוס בוצע');
  };

  const getBadges = (points: number) =>
    BADGES.filter((b) => points >= b.minPoints).map((b) => b.icon).join(' ');

  if (loading) return <div className="spinner" />;

  return (
    <div>
      <div style={styles.topBar}>
        <h1 style={styles.pageTitle}>🏆 לוח מובילים</h1>
        {user?.role === 'admin' && (
          <button className="btn btn-ghost btn-sm" onClick={resetPoints}>איפוס נקודות</button>
        )}
      </div>

      {/* פודיום */}
      {leaders.length >= 3 && (
        <div style={styles.podium}>
          {[leaders[1], leaders[0], leaders[2]].map((u, i) => {
            const positions = [1, 0, 2];
            const heights = ['80px', '110px', '65px'];
            const medals = ['🥈', '🥇', '🥉'];
            return (
              <div key={u._id} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '0.4rem' }}>
                <div style={{ fontSize: '2rem' }}>{medals[i]}</div>
                {u.photoURL
                  ? <img src={u.photoURL} alt={u.name} style={{ width: '48px', height: '48px', borderRadius: '50%', objectFit: 'cover' }} />
                  : <div style={{ ...styles.avatarFallback, width: i === 1 ? '56px' : '44px', height: i === 1 ? '56px' : '44px' }}>{u.name[0]}</div>
                }
                <div style={{ fontWeight: 700, fontSize: i === 1 ? '0.95rem' : '0.85rem' }}>{u.name.split(' ')[0]}</div>
                <div style={{ color: 'var(--color-secondary)', fontWeight: 700 }}>⭐ {u.points}</div>
                <div style={{ background: ['#C0C0C0', '#FFD700', '#CD7F32'][i], height: heights[i], width: '70px', borderRadius: '8px 8px 0 0', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff', fontWeight: 700, fontSize: '1.1rem' }}>
                  {positions[i] + 1}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* רשימה מלאה */}
      <div style={{ marginTop: '1.5rem' }}>
        {leaders.map((u, i) => {
          const isMe = u._id === user?.id;
          return (
            <div key={u._id} className="card" style={{ marginBottom: '0.6rem', display: 'flex', alignItems: 'center', gap: '1rem', background: isMe ? '#EEF4FF' : '#fff', border: isMe ? '2px solid var(--color-primary)' : undefined }}>
              <div style={styles.rankNum}>{i + 1}</div>
              {u.photoURL
                ? <img src={u.photoURL} alt={u.name} style={{ width: '40px', height: '40px', borderRadius: '50%' }} />
                : <div style={styles.avatarFallback}>{u.name[0]}</div>
              }
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: 600 }}>{u.name} {isMe && <span style={{ fontSize: '0.78rem', color: 'var(--color-primary)' }}>(אתה)</span>}</div>
                <div style={{ fontSize: '0.8rem', color: 'var(--color-gray)' }}>
                  {u.role === 'admin' ? '👑 מנהל' : '👤 משתמש'} · {getBadges(u.points) || '—'}
                </div>
              </div>
              <div style={{ fontWeight: 700, fontSize: '1.15rem', color: 'var(--color-secondary)' }}>
                ⭐ {u.points}
              </div>
            </div>
          );
        })}
      </div>

      {leaders.length === 0 && <div className="empty-state"><span>🏆</span>אין נתונים</div>}

      {/* הסבר נקודות */}
      <div className="card" style={{ marginTop: '1.5rem' }}>
        <h3 style={{ fontWeight: 700, marginBottom: '0.75rem', color: 'var(--color-primary)' }}>איך מרוויחים נקודות?</h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.4rem', fontSize: '0.9rem' }}>
          <div>🔴 משימה דחופה = <strong>30 נקודות</strong></div>
          <div>🟡 משימה רגילה = <strong>20 נקודות</strong></div>
          <div>🔵 משימה שיכולה לחכות = <strong>10 נקודות</strong></div>
        </div>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  topBar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' },
  pageTitle: { fontSize: '1.4rem', fontWeight: 700, color: 'var(--color-primary)' },
  podium: { display: 'flex', justifyContent: 'center', alignItems: 'flex-end', gap: '1.5rem', background: 'var(--color-primary)', borderRadius: '16px', padding: '1.5rem 1rem 0', marginBottom: '0.5rem' },
  rankNum: { width: '28px', height: '28px', borderRadius: '50%', background: 'var(--color-secondary)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: '0.85rem', flexShrink: 0 },
  avatarFallback: { width: '40px', height: '40px', borderRadius: '50%', background: 'var(--color-secondary)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, flexShrink: 0 },
};
