// דף מלאי הבית
import { useEffect, useState, useCallback } from 'react';
import api from '../../services/api';
import { useAuthStore } from '../../store/authStore';

interface InventoryItem {
  _id: string;
  name: string;
  category: string;
  quantity: number;
  unit?: string;
  minQuantity?: number;
  location?: string;
  expiryDate?: string;
}

export default function InventoryPage() {
  const { user } = useAuthStore();
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [search, setSearch] = useState('');
  const [form, setForm] = useState({ name: '', category: '', quantity: 1, unit: '', minQuantity: 0, location: '', expiryDate: '' });

  const load = useCallback(async () => {
    const { data } = await api.get('/inventory');
    setItems(data);
    setLoading(false);
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    await api.post('/inventory', form);
    setForm({ name: '', category: '', quantity: 1, unit: '', minQuantity: 0, location: '', expiryDate: '' });
    setShowForm(false);
    load();
  };

  const updateQty = async (id: string, quantity: number) => {
    if (quantity < 0) return;
    await api.patch(`/inventory/${id}/quantity`, { quantity });
    setItems((p) => p.map((x) => x._id === id ? { ...x, quantity } : x));
  };

  const deleteItem = async (id: string) => {
    if (!confirm('למחוק?')) return;
    await api.delete(`/inventory/${id}`);
    setItems((p) => p.filter((x) => x._id !== id));
  };

  const filtered = items.filter((i) =>
    i.name.toLowerCase().includes(search.toLowerCase()) ||
    i.category.toLowerCase().includes(search.toLowerCase())
  );

  // קיבוץ לפי קטגוריה
  const grouped = filtered.reduce<Record<string, InventoryItem[]>>((acc, item) => {
    (acc[item.category] = acc[item.category] || []).push(item);
    return acc;
  }, {});

  const lowStock = items.filter((i) => i.minQuantity && i.quantity <= i.minQuantity);

  if (loading) return <div className="spinner" />;

  return (
    <div>
      <div style={styles.topBar}>
        <h1 style={styles.pageTitle}>📦 מלאי הבית</h1>
        <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'ביטול' : '+ פריט'}
        </button>
      </div>

      {/* התראות מלאי נמוך */}
      {lowStock.length > 0 && (
        <div className="alert alert-error" style={{ marginBottom: '1rem' }}>
          ⚠️ מלאי נמוך: {lowStock.map((i) => i.name).join(' · ')}
        </div>
      )}

      {/* חיפוש */}
      <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="🔍 חיפוש..." style={{ marginBottom: '1rem', maxWidth: '300px' }} />

      {/* טופס */}
      {showForm && (
        <form onSubmit={handleAdd} className="card" style={{ marginBottom: '1rem' }}>
          <div className="grid-2">
            <div className="form-group">
              <label>שם פריט</label>
              <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
            </div>
            <div className="form-group">
              <label>קטגוריה</label>
              <input value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })} required placeholder="מזון / ניקיון / תרופות..." />
            </div>
            <div className="form-group">
              <label>כמות</label>
              <input type="number" min={0} value={form.quantity} onChange={(e) => setForm({ ...form, quantity: Number(e.target.value) })} />
            </div>
            <div className="form-group">
              <label>יחידה</label>
              <input value={form.unit} onChange={(e) => setForm({ ...form, unit: e.target.value })} placeholder="יח' / ק&quot;ג / ל'" />
            </div>
            <div className="form-group">
              <label>מינימום למלאי</label>
              <input type="number" min={0} value={form.minQuantity} onChange={(e) => setForm({ ...form, minQuantity: Number(e.target.value) })} />
            </div>
            <div className="form-group">
              <label>מיקום</label>
              <input value={form.location} onChange={(e) => setForm({ ...form, location: e.target.value })} placeholder="מחסן / מקרר..." />
            </div>
          </div>
          <button type="submit" className="btn btn-primary">הוסף</button>
        </form>
      )}

      {Object.keys(grouped).length === 0 && <div className="empty-state"><span>📦</span>אין פריטים</div>}

      {Object.entries(grouped).map(([category, catItems]) => (
        <div key={category} style={{ marginBottom: '1.25rem' }}>
          <h3 style={styles.categoryTitle}>{category}</h3>
          {catItems.map((item) => {
            const isLow = item.minQuantity !== undefined && item.quantity <= item.minQuantity;
            return (
              <div key={item._id} className="card" style={{ marginBottom: '0.5rem', display: 'flex', alignItems: 'center', gap: '0.75rem', borderRight: isLow ? '4px solid var(--color-danger)' : undefined }}>
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 600 }}>{item.name} {isLow && <span style={{ color: 'var(--color-danger)', fontSize: '0.8rem' }}>⚠️ מלאי נמוך</span>}</div>
                  {item.location && <div style={{ fontSize: '0.8rem', color: 'var(--color-gray)' }}>📍 {item.location}</div>}
                </div>
                {/* כפתורי כמות */}
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <button className="btn btn-ghost btn-sm" onClick={() => updateQty(item._id, item.quantity - 1)}>−</button>
                  <span style={{ fontWeight: 700, minWidth: '2rem', textAlign: 'center' }}>{item.quantity}</span>
                  <span style={{ fontSize: '0.8rem', color: 'var(--color-gray)' }}>{item.unit}</span>
                  <button className="btn btn-ghost btn-sm" onClick={() => updateQty(item._id, item.quantity + 1)}>+</button>
                </div>
                {user?.role === 'admin' && (
                  <button className="btn btn-danger btn-sm" onClick={() => deleteItem(item._id)}>🗑</button>
                )}
              </div>
            );
          })}
        </div>
      ))}
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  topBar: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' },
  pageTitle: { fontSize: '1.4rem', fontWeight: 700, color: 'var(--color-primary)' },
  categoryTitle: { fontSize: '0.95rem', fontWeight: 700, color: 'var(--color-secondary)', marginBottom: '0.5rem', paddingBottom: '0.25rem', borderBottom: '2px solid var(--color-light)' },
};
