// דף כניסה והרשמה
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';
import { useAuthStore } from '../../store/authStore';

type Mode = 'login' | 'create' | 'join';

export default function LoginPage() {
  const [mode, setMode] = useState<Mode>('login');
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [householdName, setHouseholdName] = useState('');
  const [inviteCode, setInviteCode] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { setAuth } = useAuthStore();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      if (mode === 'login') {
        const { data } = await api.post('/auth/login', { email, password });
        setAuth(data.user, data.token, data.household);
        navigate('/dashboard');
      } else {
        const payload = mode === 'create'
          ? { name, email, password, householdName }
          : { name, email, password, inviteCode };

        const { data } = await api.post('/auth/register', payload);

        if (!data.user.approved) {
          setError('נרשמת בהצלחה! ממתין לאישור מנהל הבית.');
          setLoading(false);
          return;
        }

        setAuth(data.user, data.token, data.household);
        navigate('/dashboard');
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || 'שגיאה, נסה שנית');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={styles.page}>
      <div style={styles.card}>
        <div style={styles.header}>
          <img src="/icon-512.png" alt="SmartHome" style={{ width: '72px', height: '72px', borderRadius: '18px', marginBottom: '0.5rem' }} />
          <h1 style={styles.title}>SmartHome</h1>
          <p style={styles.subtitle}>ניהול חכם של הבית שלך</p>
        </div>

        {/* טאבים */}
        <div style={styles.tabs}>
          <button style={{ ...styles.tab, ...(mode === 'login'  ? styles.tabActive : {}) }} onClick={() => setMode('login')}>כניסה</button>
          <button style={{ ...styles.tab, ...(mode === 'create' ? styles.tabActive : {}) }} onClick={() => setMode('create')}>בית חדש</button>
          <button style={{ ...styles.tab, ...(mode === 'join'   ? styles.tabActive : {}) }} onClick={() => setMode('join')}>הצטרפות</button>
        </div>

        {/* הסבר */}
        {mode === 'create' && <div style={styles.hint}>👑 צור בית חדש — תהיה המנהל/ת ותצרף בני משפחה</div>}
        {mode === 'join'   && <div style={styles.hint}>🏠 הצטרף לבית קיים עם קוד שקיבלת מהמנהל/ת</div>}

        {error && <div className="alert alert-error">{error}</div>}

        <form onSubmit={handleSubmit}>
          {mode !== 'login' && (
            <div className="form-group">
              <label>שם מלא</label>
              <input value={name} onChange={(e) => setName(e.target.value)} required placeholder="השם שלך" />
            </div>
          )}

          {mode === 'create' && (
            <div className="form-group">
              <label>שם הבית</label>
              <input value={householdName} onChange={(e) => setHouseholdName(e.target.value)} required placeholder='לדוגמה: משפחת כהן' />
            </div>
          )}

          {mode === 'join' && (
            <div className="form-group">
              <label>קוד הצטרפות</label>
              <input value={inviteCode} onChange={(e) => setInviteCode(e.target.value.toUpperCase())} required placeholder="ABC123" maxLength={6} style={{ letterSpacing: '0.2em', fontWeight: 700, textAlign: 'center' }} />
            </div>
          )}

          <div className="form-group">
            <label>כתובת מייל</label>
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required placeholder="example@email.com" />
          </div>
          <div className="form-group">
            <label>סיסמה</label>
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={6} placeholder="לפחות 6 תווים" />
          </div>

          <button type="submit" className="btn btn-primary" style={{ width: '100%', justifyContent: 'center', marginTop: '0.5rem' }} disabled={loading}>
            {loading ? '...' : mode === 'login' ? 'כניסה' : mode === 'create' ? 'צור בית' : 'הצטרף'}
          </button>
        </form>

        {mode === 'join' && (
          <p style={styles.note}>אחרי ההרשמה, מנהל הבית צריך לאשר אותך</p>
        )}
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: { minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'linear-gradient(135deg, #1E3A5F 0%, #2E5D8E 100%)', padding: '1rem' },
  card: { background: '#fff', borderRadius: '16px', padding: '2rem', width: '100%', maxWidth: '420px', boxShadow: '0 8px 40px rgba(0,0,0,0.2)' },
  header: { textAlign: 'center', marginBottom: '1.5rem' },
  title: { fontSize: '1.8rem', color: 'var(--color-primary)', fontWeight: 700, margin: '0.1rem 0' },
  subtitle: { color: 'var(--color-gray)', fontSize: '0.9rem' },
  tabs: { display: 'flex', background: 'var(--color-light)', borderRadius: '10px', padding: '4px', marginBottom: '1rem', gap: '3px' },
  tab: { flex: 1, padding: '0.45rem', borderRadius: '8px', background: 'transparent', color: 'var(--color-gray)', fontWeight: 600, fontSize: '0.88rem' },
  tabActive: { background: '#fff', color: 'var(--color-primary)', boxShadow: '0 1px 6px rgba(0,0,0,0.1)' },
  hint: { background: '#EEF4FF', borderRadius: '8px', padding: '0.6rem 0.9rem', fontSize: '0.85rem', marginBottom: '1rem', color: 'var(--color-primary)' },
  note: { marginTop: '1rem', fontSize: '0.8rem', color: 'var(--color-gray)', textAlign: 'center' },
};
