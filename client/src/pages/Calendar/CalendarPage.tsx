// דף לוח שנה ואירועים
import { useEffect, useState, useCallback } from 'react';
import api from '../../services/api';
import { useAuthStore } from '../../store/authStore';

interface CalEvent {
  _id: string;
  title: string;
  description?: string;
  date: string;
  allDay: boolean;
  color: string;
  createdBy?: { name: string };
}

export default function CalendarPage() {
  const { user } = useAuthStore();
  const [events, setEvents] = useState<CalEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ title: '', description: '', date: '', allDay: true, color: '#8E44AD' });

  const load = useCallback(async () => {
    const { data } = await api.get('/events');
    setEvents(data);
    setLoading(false);
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    await api.post('/events', form);
    setForm({ title: '', description: '', date: '', allDay: true, color: '#8E44AD' });
    setShowForm(false);
    load();
  };

  const deleteEvent = async (id: string) => {
    if (!confirm('למחוק אירוע?')) return;
    await api.delete(`/events/${id}`);
    setEvents((p) => p.filter((e) => e._id !== id));
  };

  // מיון לפי תאריך
  const upcoming = events.filter((e) => new Date(e.date) >= new Date()).sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
  const past     = events.filter((e) => new Date(e.date) < new Date()).sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());

  const diffDays = (dateStr: string) => {
    const diff = new Date(dateStr).getTime() - new Date().getTime();
    return Math.ceil(diff / (1000 * 60 * 60 * 24));
  };

  if (loading) return <div className="spinner" />;

  return (
    <div>
      <div style={styles.topBar}>
        <h1 style={styles.pageTitle}>📅 לוח שנה ואירועים</h1>
        <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'ביטול' : '+ אירוע'}
        </button>
      </div>

      {showForm && (
        <form onSubmit={handleAdd} className="card" style={{ marginBottom: '1rem' }}>
          <div className="grid-2">
            <div className="form-group">
              <label>כותרת</label>
              <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required placeholder="שם האירוע" />
            </div>
            <div className="form-group">
              <label>תאריך</label>
              <input type="datetime-local" value={form.date} onChange={(e) => setForm({ ...form, date: e.target.value })} required />
            </div>
            <div className="form-group">
              <label>צבע</label>
              <input type="color" value={form.color} onChange={(e) => setForm({ ...form, color: e.target.value })} style={{ height: '42px', padding: '0.2rem' }} />
            </div>
            <div className="form-group">
              <label>תיאור</label>
              <input value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} placeholder="אופציונלי" />
            </div>
          </div>
          <button type="submit" className="btn btn-primary">שמור</button>
        </form>
      )}

      {/* אירועים קרובים */}
      {upcoming.length > 0 && (
        <>
          <h2 style={styles.section}>📌 אירועים קרובים</h2>
          {upcoming.map((ev) => {
            const days = diffDays(ev.date);
            return (
              <div key={ev._id} className="card" style={{ marginBottom: '0.6rem', borderRight: `4px solid ${ev.color}` }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                  <div>
                    <div style={{ fontWeight: 700, fontSize: '1rem' }}>{ev.title}</div>
                    <div style={{ fontSize: '0.83rem', color: 'var(--color-gray)', marginTop: '0.2rem' }}>
                      📅 {new Date(ev.date).toLocaleDateString('he-IL', { weekday: 'short', day: 'numeric', month: 'long' })}
                      {!ev.allDay && ` · ${new Date(ev.date).toLocaleTimeString('he-IL', { hour: '2-digit', minute: '2-digit' })}`}
                    </div>
                    {ev.description && <div style={{ fontSize: '0.83rem', marginTop: '0.2rem' }}>{ev.description}</div>}
                  </div>
                  <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                    <span style={{
                      background: days <= 7 ? '#FDECEA' : '#EBF5FB',
                      color: days <= 7 ? 'var(--color-danger)' : '#2980B9',
                      padding: '0.2rem 0.6rem',
                      borderRadius: '20px',
                      fontSize: '0.8rem',
                      fontWeight: 700,
                    }}>
                      {days === 0 ? 'היום!' : days === 1 ? 'מחר' : `בעוד ${days} ימים`}
                    </span>
                    {user?.role === 'admin' && (
                      <button className="btn btn-danger btn-sm" onClick={() => deleteEvent(ev._id)}>🗑</button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </>
      )}

      {/* אירועים שעברו */}
      {past.length > 0 && (
        <>
          <h2 style={{ ...styles.section, color: 'var(--color-gray)' }}>🕐 אירועים שעברו</h2>
          {past.map((ev) => (
            <div key={ev._id} className="card" style={{ marginBottom: '0.5rem', opacity: 0.65, borderRight: `4px solid ${ev.color}` }}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <div>
                  <div style={{ fontWeight: 600 }}>{ev.title}</div>
                  <div style={{ fontSize: '0.82rem', color: 'var(--color-gray)' }}>
                    {new Date(ev.date).toLocaleDateString('he-IL')}
                  </div>
                </div>
                {user?.role === 'admin' && (
                  <button className="btn btn-danger btn-sm" onClick={() => deleteEvent(ev._id)}>🗑</button>
                )}
              </div>
            </div>
          ))}
        </>
      )}

      {events.length === 0 && <div className="empty-state"><span>📅</span>אין אירועים</div>}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  topBar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' },
  pageTitle: { fontSize: '1.4rem', fontWeight: 700, color: 'var(--color-primary)' },
  section: { fontSize: '1.05rem', fontWeight: 700, color: 'var(--color-primary)', margin: '1rem 0 0.6rem' },
};
