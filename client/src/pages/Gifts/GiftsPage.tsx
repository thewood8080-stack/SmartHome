// דף מתנות
import { useEffect, useState, useCallback } from 'react';
import api from '../../services/api';
import { useAuthStore } from '../../store/authStore';

interface Gift {
  _id: string;
  recipientName: string;
  occasion: string;
  date: string;
  ideas: string[];
  purchased: boolean;
  purchasedItem?: string;
  note?: string;
}

export default function GiftsPage() {
  const { user } = useAuthStore();
  const [gifts, setGifts] = useState<Gift[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ recipientName: '', occasion: '', date: '', ideas: '', note: '' });
  const [editPurchase, setEditPurchase] = useState<string | null>(null);
  const [purchasedItem, setPurchasedItem] = useState('');

  const load = useCallback(async () => {
    const { data } = await api.get('/gifts');
    setGifts(data);
    setLoading(false);
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    const ideas = form.ideas.split(',').map((s) => s.trim()).filter(Boolean);
    await api.post('/gifts', { ...form, ideas });
    setForm({ recipientName: '', occasion: '', date: '', ideas: '', note: '' });
    setShowForm(false);
    load();
  };

  const markPurchased = async (id: string) => {
    await api.put(`/gifts/${id}`, { purchased: true, purchasedItem });
    setPurchasedItem('');
    setEditPurchase(null);
    load();
  };

  const deleteGift = async (id: string) => {
    if (!confirm('למחוק?')) return;
    await api.delete(`/gifts/${id}`);
    setGifts((p) => p.filter((g) => g._id !== id));
  };

  const diffDays = (d: string) => Math.ceil((new Date(d).getTime() - new Date().getTime()) / 86400000);

  if (loading) return <div className="spinner" />;

  return (
    <div>
      <div style={styles.topBar}>
        <h1 style={styles.pageTitle}>🎁 מתנות</h1>
        <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'ביטול' : '+ מתנה'}
        </button>
      </div>

      {showForm && (
        <form onSubmit={handleAdd} className="card" style={{ marginBottom: '1rem' }}>
          <div className="grid-2">
            <div className="form-group">
              <label>שם המקבל</label>
              <input value={form.recipientName} onChange={(e) => setForm({ ...form, recipientName: e.target.value })} required placeholder="לדוגמה: סבתא רחל" />
            </div>
            <div className="form-group">
              <label>אירוע</label>
              <input value={form.occasion} onChange={(e) => setForm({ ...form, occasion: e.target.value })} required placeholder="יום הולדת / חתונה..." />
            </div>
            <div className="form-group">
              <label>תאריך האירוע</label>
              <input type="date" value={form.date} onChange={(e) => setForm({ ...form, date: e.target.value })} required />
            </div>
            <div className="form-group">
              <label>רעיונות (מופרדים בפסיק)</label>
              <input value={form.ideas} onChange={(e) => setForm({ ...form, ideas: e.target.value })} placeholder="ספר, תכשיט, כרטיסים..." />
            </div>
          </div>
          <div className="form-group">
            <label>הערה</label>
            <input value={form.note} onChange={(e) => setForm({ ...form, note: e.target.value })} placeholder="אופציונלי" />
          </div>
          <button type="submit" className="btn btn-primary">שמור</button>
        </form>
      )}

      {gifts.length === 0 && <div className="empty-state"><span>🎁</span>אין מתנות</div>}

      {gifts.map((gift) => {
        const days = diffDays(gift.date);
        const soon = days >= 0 && days <= 7;
        return (
          <div key={gift._id} className="card" style={{ marginBottom: '0.75rem', borderRight: `4px solid ${gift.purchased ? 'var(--color-success)' : soon ? 'var(--color-danger)' : 'var(--color-secondary)'}` }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: 700, fontSize: '1.05rem' }}>
                  {gift.recipientName} — {gift.occasion}
                </div>
                <div style={{ fontSize: '0.83rem', color: 'var(--color-gray)', marginTop: '0.2rem' }}>
                  📅 {new Date(gift.date).toLocaleDateString('he-IL')}
                  {days >= 0 && <span style={{ color: soon ? 'var(--color-danger)' : 'var(--color-primary)', fontWeight: 700, marginRight: '0.5rem' }}>
                    ({days === 0 ? 'היום!' : `${days} ימים`})
                  </span>}
                </div>
                {gift.ideas.length > 0 && (
                  <div style={{ marginTop: '0.4rem', display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
                    {gift.ideas.map((idea, i) => (
                      <span key={i} style={{ background: 'var(--color-light)', padding: '0.15rem 0.5rem', borderRadius: '10px', fontSize: '0.8rem' }}>{idea}</span>
                    ))}
                  </div>
                )}
                {gift.purchased && gift.purchasedItem && (
                  <div style={{ color: 'var(--color-success)', fontSize: '0.85rem', marginTop: '0.3rem' }}>
                    ✅ נקנה: {gift.purchasedItem}
                  </div>
                )}
                {/* טופס סימון רכישה */}
                {editPurchase === gift._id && (
                  <div style={{ marginTop: '0.5rem', display: 'flex', gap: '0.5rem' }}>
                    <input value={purchasedItem} onChange={(e) => setPurchasedItem(e.target.value)} placeholder="מה נקנה?" style={{ maxWidth: '200px' }} />
                    <button className="btn btn-primary btn-sm" onClick={() => markPurchased(gift._id)}>אישור</button>
                    <button className="btn btn-ghost btn-sm" onClick={() => setEditPurchase(null)}>ביטול</button>
                  </div>
                )}
              </div>
              <div style={{ display: 'flex', gap: '0.4rem' }}>
                {!gift.purchased && editPurchase !== gift._id && (
                  <button className="btn btn-primary btn-sm" onClick={() => setEditPurchase(gift._id)}>🛍 נקנה</button>
                )}
                {user?.role === 'admin' && (
                  <button className="btn btn-danger btn-sm" onClick={() => deleteGift(gift._id)}>🗑</button>
                )}
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  topBar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' },
  pageTitle: { fontSize: '1.4rem', fontWeight: 700, color: 'var(--color-primary)' },
};
