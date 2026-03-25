// דף ניהול משתמשים — מנהל בלבד
import { useEffect, useState, useCallback } from 'react';
import api from '../../services/api';
import { useAuthStore } from '../../store/authStore';

interface AdminUser {
  _id: string;
  name: string;
  email: string;
  role: 'admin' | 'member';
  approved: boolean;
  points: number;
  createdAt: string;
}

export default function AdminPage() {
  const { user } = useAuthStore();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    const { data } = await api.get('/users');
    setUsers(data);
    setLoading(false);
  }, []);

  useEffect(() => { load(); }, [load]);

  const toggleApprove = async (id: string, approved: boolean) => {
    await api.patch(`/users/${id}/approve`, { approved: !approved });
    setUsers((p) => p.map((u) => u._id === id ? { ...u, approved: !approved } : u));
  };

  const toggleRole = async (id: string, role: string) => {
    const newRole = role === 'admin' ? 'member' : 'admin';
    await api.patch(`/users/${id}/role`, { role: newRole });
    setUsers((p) => p.map((u) => u._id === id ? { ...u, role: newRole as 'admin' | 'member' } : u));
  };

  const deleteUser = async (id: string) => {
    if (!confirm('למחוק משתמש?')) return;
    await api.delete(`/users/${id}`);
    setUsers((p) => p.filter((u) => u._id !== id));
  };

  if (user?.role !== 'admin') return <div className="empty-state"><span>🔒</span>גישה למנהלים בלבד</div>;
  if (loading) return <div className="spinner" />;

  const pending = users.filter((u) => !u.approved);
  const approved = users.filter((u) => u.approved);

  return (
    <div>
      <h1 style={styles.pageTitle}>⚙️ ניהול משתמשים</h1>

      {/* ממתינים לאישור */}
      {pending.length > 0 && (
        <>
          <h2 style={{ ...styles.section, color: 'var(--color-danger)' }}>⏳ ממתינים לאישור ({pending.length})</h2>
          {pending.map((u) => (
            <div key={u._id} className="card" style={{ marginBottom: '0.6rem', borderRight: '4px solid var(--color-danger)' }}>
              <div style={styles.userRow}>
                <div style={styles.avatar}>{u.name[0]}</div>
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 700 }}>{u.name}</div>
                  <div style={{ fontSize: '0.82rem', color: 'var(--color-gray)' }}>{u.email}</div>
                  <div style={{ fontSize: '0.78rem', color: 'var(--color-gray)' }}>
                    נרשם: {new Date(u.createdAt).toLocaleDateString('he-IL')}
                  </div>
                </div>
                <div style={{ display: 'flex', gap: '0.4rem' }}>
                  <button className="btn btn-primary btn-sm" onClick={() => toggleApprove(u._id, u.approved)}>✓ אשר</button>
                  <button className="btn btn-danger btn-sm" onClick={() => deleteUser(u._id)}>מחק</button>
                </div>
              </div>
            </div>
          ))}
        </>
      )}

      {/* משתמשים פעילים */}
      <h2 style={styles.section}>👥 משתמשים פעילים ({approved.length})</h2>
      {approved.map((u) => (
        <div key={u._id} className="card" style={{ marginBottom: '0.6rem' }}>
          <div style={styles.userRow}>
            <div style={styles.avatar}>{u.name[0]}</div>
            <div style={{ flex: 1 }}>
              <div style={{ fontWeight: 700 }}>
                {u.name}
                {u.role === 'admin' && <span style={{ fontSize: '0.8rem', color: 'var(--color-secondary)', marginRight: '0.4rem' }}>👑</span>}
              </div>
              <div style={{ fontSize: '0.82rem', color: 'var(--color-gray)' }}>{u.email}</div>
              <div style={{ fontSize: '0.8rem', color: 'var(--color-secondary)' }}>⭐ {u.points} נקודות</div>
            </div>
            <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
              {u._id !== user.id && (
                <>
                  <button className="btn btn-ghost btn-sm" onClick={() => toggleRole(u._id, u.role)}>
                    {u.role === 'admin' ? 'הסר מנהל' : 'הפוך למנהל'}
                  </button>
                  <button className="btn btn-ghost btn-sm" onClick={() => toggleApprove(u._id, u.approved)}>חסום</button>
                  <button className="btn btn-danger btn-sm" onClick={() => deleteUser(u._id)}>🗑</button>
                </>
              )}
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  pageTitle: { fontSize: '1.4rem', fontWeight: 700, color: 'var(--color-primary)', marginBottom: '1.25rem' },
  section: { fontSize: '1rem', fontWeight: 700, color: 'var(--color-primary)', margin: '1rem 0 0.6rem' },
  userRow: { display: 'flex', alignItems: 'center', gap: '0.75rem' },
  avatar: { width: '40px', height: '40px', borderRadius: '50%', background: 'var(--color-primary)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, flexShrink: 0 },
};
