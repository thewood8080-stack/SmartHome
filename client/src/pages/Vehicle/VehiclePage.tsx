// דף ניהול רכב
import { useEffect, useState, useCallback } from 'react';
import api from '../../services/api';
import { useAuthStore } from '../../store/authStore';

interface Vehicle {
  _id: string;
  name: string;
  plateNumber?: string;
  year?: number;
  lastService?: string;
  nextService?: string;
  insurance?: string;
  test?: string;
  fuelType?: string;
  notes?: string;
}

export default function VehiclePage() {
  const { user } = useAuthStore();
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: '', plateNumber: '', year: '', lastService: '', nextService: '', insurance: '', test: '', fuelType: '', notes: '' });

  const load = useCallback(async () => {
    const { data } = await api.get('/vehicles');
    setVehicles(data);
    setLoading(false);
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    await api.post('/vehicles', { ...form, year: form.year ? Number(form.year) : undefined });
    setForm({ name: '', plateNumber: '', year: '', lastService: '', nextService: '', insurance: '', test: '', fuelType: '', notes: '' });
    setShowForm(false);
    load();
  };

  const deleteVehicle = async (id: string) => {
    if (!confirm('למחוק רכב?')) return;
    await api.delete(`/vehicles/${id}`);
    setVehicles((p) => p.filter((v) => v._id !== id));
  };

  const daysUntil = (d?: string) => {
    if (!d) return null;
    return Math.ceil((new Date(d).getTime() - new Date().getTime()) / 86400000);
  };

  const AlertBadge = ({ days, label }: { days: number | null; label: string }) => {
    if (days === null) return null;
    const color = days < 0 ? 'var(--color-danger)' : days <= 30 ? '#D4AC0D' : 'var(--color-success)';
    return (
      <span style={{ background: color + '20', color, padding: '0.2rem 0.6rem', borderRadius: '10px', fontSize: '0.78rem', fontWeight: 600, marginLeft: '0.4rem' }}>
        {label}: {days < 0 ? `עבר לפני ${Math.abs(days)} ימים` : days === 0 ? 'היום!' : `${days} ימים`}
      </span>
    );
  };

  if (loading) return <div className="spinner" />;

  return (
    <div>
      <div style={styles.topBar}>
        <h1 style={styles.pageTitle}>🚗 ניהול רכב</h1>
        <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'ביטול' : '+ רכב'}
        </button>
      </div>

      {showForm && (
        <form onSubmit={handleAdd} className="card" style={{ marginBottom: '1rem' }}>
          <div className="grid-2">
            <div className="form-group">
              <label>שם / דגם</label>
              <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required placeholder="לדוגמה: טויוטה קורולה" />
            </div>
            <div className="form-group">
              <label>לוחית רישוי</label>
              <input value={form.plateNumber} onChange={(e) => setForm({ ...form, plateNumber: e.target.value })} placeholder="123-45-678" />
            </div>
            <div className="form-group">
              <label>שנת ייצור</label>
              <input type="number" value={form.year} onChange={(e) => setForm({ ...form, year: e.target.value })} placeholder="2020" />
            </div>
            <div className="form-group">
              <label>סוג דלק</label>
              <input value={form.fuelType} onChange={(e) => setForm({ ...form, fuelType: e.target.value })} placeholder="בנזין / דיזל / חשמלי" />
            </div>
            <div className="form-group">
              <label>טיפול אחרון</label>
              <input type="date" value={form.lastService} onChange={(e) => setForm({ ...form, lastService: e.target.value })} />
            </div>
            <div className="form-group">
              <label>טיפול הבא</label>
              <input type="date" value={form.nextService} onChange={(e) => setForm({ ...form, nextService: e.target.value })} />
            </div>
            <div className="form-group">
              <label>ביטוח — תפוגה</label>
              <input type="date" value={form.insurance} onChange={(e) => setForm({ ...form, insurance: e.target.value })} />
            </div>
            <div className="form-group">
              <label>טסט — תאריך</label>
              <input type="date" value={form.test} onChange={(e) => setForm({ ...form, test: e.target.value })} />
            </div>
          </div>
          <div className="form-group">
            <label>הערות</label>
            <textarea value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} rows={2} />
          </div>
          <button type="submit" className="btn btn-primary">שמור</button>
        </form>
      )}

      {vehicles.length === 0 && <div className="empty-state"><span>🚗</span>אין רכבים</div>}

      {vehicles.map((v) => (
        <div key={v._id} className="card" style={{ marginBottom: '1rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <div>
              <div style={{ fontSize: '1.2rem', fontWeight: 700 }}>🚗 {v.name}</div>
              {v.plateNumber && <div style={{ fontSize: '0.85rem', color: 'var(--color-gray)' }}>לוחית: {v.plateNumber} {v.year && `· ${v.year}`}</div>}
              {v.fuelType && <div style={{ fontSize: '0.83rem', color: 'var(--color-gray)' }}>⛽ {v.fuelType}</div>}
            </div>
            {user?.role === 'admin' && (
              <button className="btn btn-danger btn-sm" onClick={() => deleteVehicle(v._id)}>🗑</button>
            )}
          </div>
          <div style={{ marginTop: '0.75rem', display: 'flex', flexWrap: 'wrap', gap: '0.4rem' }}>
            <AlertBadge days={daysUntil(v.test)} label="טסט" />
            <AlertBadge days={daysUntil(v.insurance)} label="ביטוח" />
            <AlertBadge days={daysUntil(v.nextService)} label="טיפול" />
          </div>
          {v.notes && <div style={{ fontSize: '0.83rem', marginTop: '0.5rem', color: 'var(--color-gray)' }}>{v.notes}</div>}
        </div>
      ))}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  topBar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' },
  pageTitle: { fontSize: '1.4rem', fontWeight: 700, color: 'var(--color-primary)' },
};
