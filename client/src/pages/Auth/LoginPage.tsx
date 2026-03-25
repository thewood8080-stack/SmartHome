// דף כניסה והרשמה
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';
import { useAuthStore } from '../../store/authStore';
import { STRINGS } from '../../utils/strings';

type Mode = 'login' | 'register';

export default function LoginPage() {
  const [mode, setMode] = useState<Mode>('login');
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { setAuth } = useAuthStore();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const endpoint = mode === 'login' ? '/auth/login' : '/auth/register';
      const payload = mode === 'login' ? { email, password } : { name, email, password };
      const { data } = await api.post(endpoint, payload);

      if (!data.user.approved) {
        setError(STRINGS.auth.notApproved);
        setLoading(false);
        return;
      }

      setAuth(data.user, data.token);
      navigate('/dashboard');
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
          <span style={{ fontSize: '2.5rem' }}>🏡</span>
          <h1 style={styles.title}>{STRINGS.app.name}</h1>
          <p style={styles.subtitle}>{STRINGS.app.tagline}</p>
        </div>

        <div style={styles.tabs}>
          <button
            style={{ ...styles.tab, ...(mode === 'login' ? styles.tabActive : {}) }}
            onClick={() => setMode('login')}
          >כניסה</button>
          <button
            style={{ ...styles.tab, ...(mode === 'register' ? styles.tabActive : {}) }}
            onClick={() => setMode('register')}
          >הרשמה</button>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <form onSubmit={handleSubmit}>
          {mode === 'register' && (
            <div className="form-group">
              <label>שם מלא</label>
              <input
                type="text"
                placeholder="הכנס שם מלא"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
            </div>
          )}
          <div className="form-group">
            <label>כתובת מייל</label>
            <input
              type="email"
              placeholder="example@email.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>
          <div className="form-group">
            <label>סיסמה</label>
            <input
              type="password"
              placeholder="לפחות 6 תווים"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={6}
            />
          </div>
          <button
            type="submit"
            className="btn btn-primary"
            style={{ width: '100%', justifyContent: 'center', marginTop: '0.5rem' }}
            disabled={loading}
          >
            {loading ? '...' : mode === 'login' ? 'כניסה' : 'הרשמה'}
          </button>
        </form>

        {mode === 'register' && (
          <p style={styles.note}>
            משתמש חדש יאושר על ידי מנהל הבית לפני הכניסה
          </p>
        )}
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: {
    minHeight: '100vh',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    background: 'linear-gradient(135deg, #1E3A5F 0%, #2E5D8E 100%)',
    padding: '1rem',
  },
  card: {
    background: '#fff',
    borderRadius: '16px',
    padding: '2rem',
    width: '100%',
    maxWidth: '420px',
    boxShadow: '0 8px 40px rgba(0,0,0,0.2)',
  },
  header: {
    textAlign: 'center',
    marginBottom: '1.5rem',
  },
  title: {
    fontSize: '1.8rem',
    color: 'var(--color-primary)',
    fontWeight: 700,
    margin: '0.25rem 0 0.1rem',
  },
  subtitle: {
    color: 'var(--color-gray)',
    fontSize: '0.9rem',
  },
  tabs: {
    display: 'flex',
    background: 'var(--color-light)',
    borderRadius: '10px',
    padding: '4px',
    marginBottom: '1.25rem',
    gap: '4px',
  },
  tab: {
    flex: 1,
    padding: '0.5rem',
    borderRadius: '8px',
    background: 'transparent',
    color: 'var(--color-gray)',
    fontWeight: 600,
    fontSize: '0.95rem',
  },
  tabActive: {
    background: '#fff',
    color: 'var(--color-primary)',
    boxShadow: '0 1px 6px rgba(0,0,0,0.1)',
  },
  note: {
    marginTop: '1rem',
    fontSize: '0.8rem',
    color: 'var(--color-gray)',
    textAlign: 'center',
  },
};
