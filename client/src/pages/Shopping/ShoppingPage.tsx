// דף רשימת קניות
import { useEffect, useState, useCallback } from 'react';
import api from '../../services/api';
import { useAuthStore } from '../../store/authStore';
import { useSocketEvent } from '../../hooks/useSocket';

interface ShoppingItem {
  _id: string;
  name: string;
  quantity: number;
  unit?: string;
  category?: string;
  urgent: boolean;
  bought: boolean;
  addedBy?: { name: string };
  boughtBy?: { name: string };
}

export default function ShoppingPage() {
  const { user } = useAuthStore();
  const [items, setItems] = useState<ShoppingItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState({ name: '', quantity: 1, unit: '', category: '', urgent: false });
  const [showForm, setShowForm] = useState(false);

  const load = useCallback(async () => {
    const { data } = await api.get('/shopping');
    setItems(data);
    setLoading(false);
  }, []);

  useEffect(() => { load(); }, [load]);

  useSocketEvent<ShoppingItem>('shopping:created', (item) => setItems((p) => [item, ...p]));
  useSocketEvent<ShoppingItem>('shopping:updated', (item) => setItems((p) => p.map((x) => x._id === item._id ? item : x)));
  useSocketEvent<{ id: string }>('shopping:deleted', ({ id }) => setItems((p) => p.filter((x) => x._id !== id)));
  useSocketEvent<object>('shopping:cleared', () => setItems((p) => p.filter((x) => !x.bought)));

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    await api.post('/shopping', form);
    setForm({ name: '', quantity: 1, unit: '', category: '', urgent: false });
    setShowForm(false);
  };

  const toggleBought = async (id: string, bought: boolean) => {
    await api.patch(`/shopping/${id}/bought`, { bought: !bought });
  };

  const deleteItem = async (id: string) => {
    await api.delete(`/shopping/${id}`);
  };

  const clearBought = async () => {
    if (!confirm('למחוק את כל הפריטים שנקנו?')) return;
    await api.delete('/shopping/clear/bought');
  };

  const pending = items.filter((i) => !i.bought);
  const bought  = items.filter((i) => i.bought);

  if (loading) return <div className="spinner" />;

  return (
    <div>
      <div style={styles.topBar}>
        <h1 style={styles.pageTitle}>🛒 רשימת קניות</h1>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          {bought.length > 0 && (
            <button className="btn btn-ghost btn-sm" onClick={clearBought}>נקה שנקנו</button>
          )}
          <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
            {showForm ? 'ביטול' : '+ פריט'}
          </button>
        </div>
      </div>

      {/* טופס הוספה */}
      {showForm && (
        <form onSubmit={handleAdd} className="card" style={{ marginBottom: '1rem' }}>
          <div className="grid-2">
            <div className="form-group">
              <label>שם מוצר</label>
              <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required placeholder="לדוגמה: חלב" />
            </div>
            <div className="form-group">
              <label>כמות</label>
              <input type="number" min={1} value={form.quantity} onChange={(e) => setForm({ ...form, quantity: Number(e.target.value) })} />
            </div>
            <div className="form-group">
              <label>יחידה</label>
              <input value={form.unit} onChange={(e) => setForm({ ...form, unit: e.target.value })} placeholder="ק&quot;ג / יחידה / ליטר" />
            </div>
            <div className="form-group">
              <label>קטגוריה</label>
              <input value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })} placeholder="מזון / ניקיון..." />
            </div>
          </div>
          <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem', cursor: 'pointer' }}>
            <input type="checkbox" checked={form.urgent} onChange={(e) => setForm({ ...form, urgent: e.target.checked })} style={{ width: 'auto' }} />
            <span style={{ fontWeight: 600 }}>⚡ דחוף</span>
          </label>
          <button type="submit" className="btn btn-primary">הוסף</button>
        </form>
      )}

      {/* לא נקנה */}
      {pending.length === 0 && bought.length === 0 && (
        <div className="empty-state"><span>🛒</span>הרשימה ריקה</div>
      )}

      {pending.map((item) => (
        <ShoppingRow key={item._id} item={item} onToggle={toggleBought} onDelete={deleteItem} isAdmin={user?.role === 'admin'} />
      ))}

      {/* נקנה */}
      {bought.length > 0 && (
        <>
          <h3 style={{ color: 'var(--color-success)', margin: '1rem 0 0.5rem', fontSize: '0.95rem' }}>
            ✅ נקנה ({bought.length})
          </h3>
          {bought.map((item) => (
            <ShoppingRow key={item._id} item={item} onToggle={toggleBought} onDelete={deleteItem} isAdmin={user?.role === 'admin'} />
          ))}
        </>
      )}
    </div>
  );
}

function ShoppingRow({ item, onToggle, onDelete, isAdmin }: {
  item: ShoppingItem;
  onToggle: (id: string, bought: boolean) => void;
  onDelete: (id: string) => void;
  isAdmin: boolean;
}) {
  return (
    <div className="card" style={{ marginBottom: '0.5rem', display: 'flex', alignItems: 'center', gap: '0.75rem', opacity: item.bought ? 0.6 : 1 }}>
      <button
        onClick={() => onToggle(item._id, item.bought)}
        style={{ fontSize: '1.3rem', background: 'none', flexShrink: 0 }}
      >
        {item.bought ? '☑️' : '⬜'}
      </button>
      <div style={{ flex: 1 }}>
        <span style={{ fontWeight: 600, textDecoration: item.bought ? 'line-through' : 'none' }}>
          {item.urgent && !item.bought && '⚡ '}{item.name}
        </span>
        <span style={{ color: 'var(--color-gray)', fontSize: '0.85rem', marginRight: '0.5rem' }}>
          {item.quantity} {item.unit}
        </span>
        {item.category && <span style={{ fontSize: '0.8rem', color: 'var(--color-secondary)' }}> · {item.category}</span>}
        {item.boughtBy && <span style={{ fontSize: '0.78rem', color: 'var(--color-success)', display: 'block' }}>נקנה ע"י {item.boughtBy.name}</span>}
      </div>
      {isAdmin && (
        <button className="btn btn-danger btn-sm" onClick={() => onDelete(item._id)}>🗑</button>
      )}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  topBar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' },
  pageTitle: { fontSize: '1.4rem', fontWeight: 700, color: 'var(--color-primary)' },
};
