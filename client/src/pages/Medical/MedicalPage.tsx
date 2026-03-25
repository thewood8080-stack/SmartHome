// דף תיק רפואי
import { useEffect, useState, useCallback } from 'react';
import api from '../../services/api';
import { useAuthStore } from '../../store/authStore';

interface MedRecord {
  _id: string;
  memberId: { _id: string; name: string };
  type: 'appointment' | 'medication' | 'vaccine' | 'note';
  title: string;
  date: string;
  doctor?: string;
  clinic?: string;
  notes?: string;
  nextAppointment?: string;
}

const TYPE_LABEL: Record<string, string> = { appointment: 'תור', medication: 'תרופה', vaccine: 'חיסון', note: 'הערה' };
const TYPE_ICON:  Record<string, string> = { appointment: '🏥', medication: '💊', vaccine: '💉', note: '📝' };

export default function MedicalPage() {
  const { user } = useAuthStore();
  const [records, setRecords] = useState<MedRecord[]>([]);
  const [users, setUsers] = useState<{ _id: string; name: string }[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [selectedMember, setSelectedMember] = useState('');
  const [form, setForm] = useState({ memberId: '', type: 'appointment', title: '', date: '', doctor: '', clinic: '', notes: '', nextAppointment: '' });

  const load = useCallback(async () => {
    const [recs, us] = await Promise.all([api.get('/medical'), api.get('/users')]);
    setRecords(recs.data);
    setUsers(us.data);
    setLoading(false);
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    await api.post('/medical', form);
    setForm({ memberId: '', type: 'appointment', title: '', date: '', doctor: '', clinic: '', notes: '', nextAppointment: '' });
    setShowForm(false);
    load();
  };

  const deleteRecord = async (id: string) => {
    if (!confirm('למחוק?')) return;
    await api.delete(`/medical/${id}`);
    setRecords((p) => p.filter((r) => r._id !== id));
  };

  const filtered = selectedMember ? records.filter((r) => r.memberId?._id === selectedMember) : records;

  if (loading) return <div className="spinner" />;

  return (
    <div>
      <div style={styles.topBar}>
        <h1 style={styles.pageTitle}>🏥 תיק רפואי</h1>
        <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'ביטול' : '+ רשומה'}
        </button>
      </div>

      {/* פילטר לפי בן משפחה */}
      <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem', flexWrap: 'wrap' }}>
        <button onClick={() => setSelectedMember('')} style={{ ...styles.filterBtn, ...(selectedMember === '' ? styles.filterActive : {}) }}>
          הכל
        </button>
        {users.map((u) => (
          <button key={u._id} onClick={() => setSelectedMember(u._id)}
            style={{ ...styles.filterBtn, ...(selectedMember === u._id ? styles.filterActive : {}) }}>
            {u.name}
          </button>
        ))}
      </div>

      {showForm && (
        <form onSubmit={handleAdd} className="card" style={{ marginBottom: '1rem' }}>
          <div className="grid-2">
            <div className="form-group">
              <label>בן משפחה</label>
              <select value={form.memberId} onChange={(e) => setForm({ ...form, memberId: e.target.value })} required>
                <option value="">בחר...</option>
                {users.map((u) => <option key={u._id} value={u._id}>{u.name}</option>)}
              </select>
            </div>
            <div className="form-group">
              <label>סוג</label>
              <select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value })}>
                <option value="appointment">תור לרופא</option>
                <option value="medication">תרופה</option>
                <option value="vaccine">חיסון</option>
                <option value="note">הערה</option>
              </select>
            </div>
            <div className="form-group">
              <label>כותרת</label>
              <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required placeholder="לדוגמה: רופא משפחה" />
            </div>
            <div className="form-group">
              <label>תאריך</label>
              <input type="date" value={form.date} onChange={(e) => setForm({ ...form, date: e.target.value })} required />
            </div>
            <div className="form-group">
              <label>רופא</label>
              <input value={form.doctor} onChange={(e) => setForm({ ...form, doctor: e.target.value })} placeholder="שם הרופא" />
            </div>
            <div className="form-group">
              <label>מרפאה / בית חולים</label>
              <input value={form.clinic} onChange={(e) => setForm({ ...form, clinic: e.target.value })} />
            </div>
          </div>
          <div className="form-group">
            <label>הערות</label>
            <textarea value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} rows={2} />
          </div>
          <button type="submit" className="btn btn-primary">שמור</button>
        </form>
      )}

      {filtered.length === 0 && <div className="empty-state"><span>🏥</span>אין רשומות</div>}

      {filtered.map((rec) => (
        <div key={rec._id} className="card" style={{ marginBottom: '0.65rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <div>
              <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                <span style={{ fontSize: '1.3rem' }}>{TYPE_ICON[rec.type]}</span>
                <span style={{ fontWeight: 700 }}>{rec.title}</span>
                <span className="badge badge-medium">{TYPE_LABEL[rec.type]}</span>
              </div>
              <div style={{ fontSize: '0.83rem', color: 'var(--color-gray)', marginTop: '0.25rem' }}>
                👤 {rec.memberId?.name} · 📅 {new Date(rec.date).toLocaleDateString('he-IL')}
                {rec.doctor && ` · 👨‍⚕️ ${rec.doctor}`}
                {rec.clinic && ` · 🏥 ${rec.clinic}`}
              </div>
              {rec.notes && <div style={{ fontSize: '0.83rem', marginTop: '0.2rem' }}>{rec.notes}</div>}
            </div>
            {user?.role === 'admin' && (
              <button className="btn btn-danger btn-sm" onClick={() => deleteRecord(rec._id)}>🗑</button>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  topBar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' },
  pageTitle: { fontSize: '1.4rem', fontWeight: 700, color: 'var(--color-primary)' },
  filterBtn: { padding: '0.35rem 0.9rem', borderRadius: '20px', background: 'var(--color-light)', fontSize: '0.85rem', fontWeight: 600 },
  filterActive: { background: 'var(--color-primary)', color: '#fff' },
};
