// דף תקציב
import { useEffect, useState, useCallback } from 'react';
import api from '../../services/api';

interface BudgetRecord {
  _id: string;
  title: string;
  amount: number;
  type: 'income' | 'expense';
  category: string;
  date: string;
  note?: string;
}

const CATEGORIES = ['משכורת', 'אוכל', 'חשבונות', 'קניות', 'בידור', 'בריאות', 'חינוך', 'רכב', 'אחר'];

export default function BudgetPage() {
  const [records, setRecords] = useState<BudgetRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ title: '', amount: '', type: 'expense', category: 'אחר', date: new Date().toISOString().slice(0,10), note: '' });
  const now = new Date();
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [year]  = useState(now.getFullYear());

  const load = useCallback(async () => {
    const { data } = await api.get(`/budget?month=${month}&year=${year}`);
    setRecords(data);
    setLoading(false);
  }, [month, year]);

  useEffect(() => { load(); }, [load]);

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    await api.post('/budget', { ...form, amount: Number(form.amount) });
    setForm({ title: '', amount: '', type: 'expense', category: 'אחר', date: new Date().toISOString().slice(0,10), note: '' });
    setShowForm(false);
    load();
  };

  const deleteRecord = async (id: string) => {
    if (!confirm('למחוק?')) return;
    await api.delete(`/budget/${id}`);
    setRecords((p) => p.filter((r) => r._id !== id));
  };

  const totalIncome  = records.filter((r) => r.type === 'income').reduce((s, r) => s + r.amount, 0);
  const totalExpense = records.filter((r) => r.type === 'expense').reduce((s, r) => s + r.amount, 0);
  const balance      = totalIncome - totalExpense;

  const MONTHS = ['ינואר','פברואר','מרץ','אפריל','מאי','יוני','יולי','אוגוסט','ספטמבר','אוקטובר','נובמבר','דצמבר'];

  if (loading) return <div className="spinner" />;

  return (
    <div>
      <div style={styles.topBar}>
        <h1 style={styles.pageTitle}>💰 תקציב</h1>
        <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'ביטול' : '+ רשומה'}
        </button>
      </div>

      {/* בחירת חודש */}
      <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1.25rem', flexWrap: 'wrap' }}>
        {MONTHS.map((m, i) => (
          <button key={i} onClick={() => setMonth(i + 1)}
            style={{ ...styles.monthBtn, ...(month === i + 1 ? styles.monthActive : {}) }}
          >{m}</button>
        ))}
      </div>

      {/* סיכום */}
      <div className="grid-3" style={{ marginBottom: '1.25rem' }}>
        <div className="card" style={{ borderTop: '4px solid var(--color-success)', textAlign: 'center' }}>
          <div style={{ color: 'var(--color-gray)', fontSize: '0.85rem' }}>הכנסות</div>
          <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--color-success)' }}>₪{totalIncome.toLocaleString()}</div>
        </div>
        <div className="card" style={{ borderTop: '4px solid var(--color-danger)', textAlign: 'center' }}>
          <div style={{ color: 'var(--color-gray)', fontSize: '0.85rem' }}>הוצאות</div>
          <div style={{ fontSize: '1.5rem', fontWeight: 700, color: 'var(--color-danger)' }}>₪{totalExpense.toLocaleString()}</div>
        </div>
        <div className="card" style={{ borderTop: `4px solid ${balance >= 0 ? 'var(--color-success)' : 'var(--color-danger)'}`, textAlign: 'center' }}>
          <div style={{ color: 'var(--color-gray)', fontSize: '0.85rem' }}>יתרה</div>
          <div style={{ fontSize: '1.5rem', fontWeight: 700, color: balance >= 0 ? 'var(--color-success)' : 'var(--color-danger)' }}>
            ₪{balance.toLocaleString()}
          </div>
        </div>
      </div>

      {/* טופס */}
      {showForm && (
        <form onSubmit={handleAdd} className="card" style={{ marginBottom: '1rem' }}>
          <div className="grid-2">
            <div className="form-group">
              <label>כותרת</label>
              <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required placeholder="לדוגמה: משכורת / סופר" />
            </div>
            <div className="form-group">
              <label>סכום ₪</label>
              <input type="number" min={0} value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} required />
            </div>
            <div className="form-group">
              <label>סוג</label>
              <select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value })}>
                <option value="expense">הוצאה</option>
                <option value="income">הכנסה</option>
              </select>
            </div>
            <div className="form-group">
              <label>קטגוריה</label>
              <select value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })}>
                {CATEGORIES.map((c) => <option key={c}>{c}</option>)}
              </select>
            </div>
            <div className="form-group">
              <label>תאריך</label>
              <input type="date" value={form.date} onChange={(e) => setForm({ ...form, date: e.target.value })} required />
            </div>
            <div className="form-group">
              <label>הערה</label>
              <input value={form.note} onChange={(e) => setForm({ ...form, note: e.target.value })} placeholder="אופציונלי" />
            </div>
          </div>
          <button type="submit" className="btn btn-primary">שמור</button>
        </form>
      )}

      {/* רשימה */}
      {records.length === 0
        ? <div className="empty-state"><span>💰</span>אין רשומות לחודש זה</div>
        : records.map((r) => (
          <div key={r._id} className="card" style={{ marginBottom: '0.5rem', display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <span style={{ fontSize: '1.4rem' }}>{r.type === 'income' ? '📈' : '📉'}</span>
            <div style={{ flex: 1 }}>
              <div style={{ fontWeight: 600 }}>{r.title}</div>
              <div style={{ fontSize: '0.82rem', color: 'var(--color-gray)' }}>
                {r.category} · {new Date(r.date).toLocaleDateString('he-IL')}
              </div>
            </div>
            <div style={{ fontWeight: 700, fontSize: '1.1rem', color: r.type === 'income' ? 'var(--color-success)' : 'var(--color-danger)' }}>
              {r.type === 'income' ? '+' : '-'}₪{r.amount.toLocaleString()}
            </div>
            <button className="btn btn-danger btn-sm" onClick={() => deleteRecord(r._id)}>🗑</button>
          </div>
        ))
      }
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  topBar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' },
  pageTitle: { fontSize: '1.4rem', fontWeight: 700, color: 'var(--color-primary)' },
  monthBtn: { padding: '0.3rem 0.7rem', borderRadius: '20px', background: 'var(--color-light)', fontSize: '0.82rem', fontWeight: 600 },
  monthActive: { background: 'var(--color-primary)', color: '#fff' },
};
