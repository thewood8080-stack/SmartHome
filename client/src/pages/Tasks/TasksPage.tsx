// דף משימות
import { useEffect, useState, useCallback } from 'react';
import api from '../../services/api';
import { useAuthStore } from '../../store/authStore';
import { useSocketEvent } from '../../hooks/useSocket';

interface Task {
  _id: string;
  title: string;
  description?: string;
  priority: 'low' | 'medium' | 'high';
  status: 'todo' | 'inprogress' | 'done';
  points: number;
  assignedTo?: { _id: string; name: string };
  createdBy?: { name: string };
  dueDate?: string;
}

const PRIORITY_LABEL: Record<string, string> = { high: 'דחוף', medium: 'רגיל', low: 'יכול לחכות' };
const STATUS_LABEL: Record<string, string>   = { todo: 'לביצוע', inprogress: 'בתהליך', done: 'בוצע' };

export default function TasksPage() {
  const { user } = useAuthStore();
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [filter, setFilter] = useState<'all' | 'todo' | 'done'>('all');
  const [form, setForm] = useState({ title: '', description: '', priority: 'medium', dueDate: '' });
  const [users, setUsers] = useState<{ _id: string; name: string }[]>([]);
  const [assignedTo, setAssignedTo] = useState('');

  const load = useCallback(async () => {
    const { data } = await api.get('/tasks');
    setTasks(data);
    setLoading(false);
  }, []);

  useEffect(() => {
    load();
    api.get('/users').then(({ data }) => setUsers(data));
  }, [load]);

  // עדכון בזמן אמת
  useSocketEvent<Task>('task:created', (t) => setTasks((prev) => [t, ...prev]));
  useSocketEvent<Task>('task:updated', (t) => setTasks((prev) => prev.map((x) => x._id === t._id ? t : x)));
  useSocketEvent<{ id: string }>('task:deleted', ({ id }) => setTasks((prev) => prev.filter((x) => x._id !== id)));

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    await api.post('/tasks', { ...form, assignedTo: assignedTo || undefined });
    setForm({ title: '', description: '', priority: 'medium', dueDate: '' });
    setAssignedTo('');
    setShowForm(false);
  };

  const setStatus = async (id: string, status: string) => {
    await api.patch(`/tasks/${id}/status`, { status });
  };

  const deleteTask = async (id: string) => {
    if (!confirm('למחוק את המשימה?')) return;
    await api.delete(`/tasks/${id}`);
  };

  const filtered = tasks.filter((t) =>
    filter === 'all' ? true : filter === 'done' ? t.status === 'done' : t.status !== 'done'
  );

  if (loading) return <div className="spinner" />;

  return (
    <div>
      <div style={styles.topBar}>
        <h1 style={styles.pageTitle}>✅ משימות בית</h1>
        {user?.role === 'admin' && (
          <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
            {showForm ? 'ביטול' : '+ משימה חדשה'}
          </button>
        )}
      </div>

      {/* טופס יצירה */}
      {showForm && (
        <form onSubmit={handleCreate} className="card" style={{ marginBottom: '1rem' }}>
          <h3 style={{ marginBottom: '1rem', color: 'var(--color-primary)' }}>משימה חדשה</h3>
          <div className="grid-2">
            <div className="form-group">
              <label>כותרת</label>
              <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required placeholder="שם המשימה" />
            </div>
            <div className="form-group">
              <label>עדיפות</label>
              <select value={form.priority} onChange={(e) => setForm({ ...form, priority: e.target.value })}>
                <option value="high">דחוף (30 נקודות)</option>
                <option value="medium">רגיל (20 נקודות)</option>
                <option value="low">יכול לחכות (10 נקודות)</option>
              </select>
            </div>
          </div>
          <div className="grid-2">
            <div className="form-group">
              <label>שייך ל</label>
              <select value={assignedTo} onChange={(e) => setAssignedTo(e.target.value)}>
                <option value="">כל בני הבית</option>
                {users.map((u) => <option key={u._id} value={u._id}>{u.name}</option>)}
              </select>
            </div>
            <div className="form-group">
              <label>תאריך יעד</label>
              <input type="date" value={form.dueDate} onChange={(e) => setForm({ ...form, dueDate: e.target.value })} />
            </div>
          </div>
          <div className="form-group">
            <label>תיאור</label>
            <textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} rows={2} placeholder="תיאור אופציונלי" />
          </div>
          <button type="submit" className="btn btn-primary">שמור</button>
        </form>
      )}

      {/* פילטרים */}
      <div style={styles.filters}>
        {(['all', 'todo', 'done'] as const).map((f) => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            style={{ ...styles.filterBtn, ...(filter === f ? styles.filterActive : {}) }}
          >
            {f === 'all' ? 'הכל' : f === 'done' ? 'בוצעו' : 'פתוחות'}
          </button>
        ))}
      </div>

      {/* רשימה */}
      {filtered.length === 0
        ? <div className="empty-state"><span>📋</span>אין משימות</div>
        : filtered.map((task) => (
          <div key={task._id} className="card" style={{ marginBottom: '0.75rem', opacity: task.status === 'done' ? 0.7 : 1 }}>
            <div style={styles.taskRow}>
              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
                  <span style={{ fontWeight: 600, textDecoration: task.status === 'done' ? 'line-through' : 'none' }}>
                    {task.title}
                  </span>
                  <span className={`badge badge-${task.priority}`}>{PRIORITY_LABEL[task.priority]}</span>
                  <span className={`badge badge-${task.status === 'done' ? 'done' : 'medium'}`}>{STATUS_LABEL[task.status]}</span>
                  <span style={{ fontSize: '0.8rem', color: 'var(--color-secondary)' }}>⭐ {task.points}</span>
                </div>
                {task.assignedTo && (
                  <div style={{ fontSize: '0.82rem', color: 'var(--color-gray)', marginTop: '0.25rem' }}>
                    👤 {task.assignedTo.name}
                  </div>
                )}
                {task.dueDate && (
                  <div style={{ fontSize: '0.8rem', color: 'var(--color-gray)' }}>
                    📅 {new Date(task.dueDate).toLocaleDateString('he-IL')}
                  </div>
                )}
              </div>
              <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
                {task.status !== 'done' && (
                  <button className="btn btn-primary btn-sm" onClick={() => setStatus(task._id, 'done')}>
                    ✓ בוצע
                  </button>
                )}
                {task.status === 'todo' && (
                  <button className="btn btn-ghost btn-sm" onClick={() => setStatus(task._id, 'inprogress')}>
                    בתהליך
                  </button>
                )}
                {user?.role === 'admin' && (
                  <button className="btn btn-danger btn-sm" onClick={() => deleteTask(task._id)}>🗑</button>
                )}
              </div>
            </div>
          </div>
        ))
      }
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  topBar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' },
  pageTitle: { fontSize: '1.4rem', fontWeight: 700, color: 'var(--color-primary)' },
  filters: { display: 'flex', gap: '0.5rem', marginBottom: '1rem' },
  filterBtn: { padding: '0.4rem 1rem', borderRadius: '20px', background: 'var(--color-light)', color: 'var(--color-text)', fontWeight: 600, fontSize: '0.88rem' },
  filterActive: { background: 'var(--color-primary)', color: '#fff' },
  taskRow: { display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '1rem' },
};
