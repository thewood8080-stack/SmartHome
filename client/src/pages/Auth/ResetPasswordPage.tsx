// דף איפוס סיסמה — נפתח מהקישור שנשלח במייל
import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import api from '../../services/api';

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const email = searchParams.get('email') ?? '';
  const token = searchParams.get('token') ?? '';

  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  // קישור חסר פרמטרים אינו שמיש — אין טעם להציג טופס.
  const linkIsValid = email !== '' && token !== '';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await api.post('/auth/reset-password', { email, token, newPassword: password });
      navigate('/login');
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
          <h1 style={styles.title}>סיסמה חדשה</h1>
          {linkIsValid && <p style={styles.subtitle}>{email}</p>}
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        {linkIsValid ? (
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>סיסמה חדשה</label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                minLength={8}
                placeholder="לפחות 8 תווים, אות גדולה וספרה"
              />
            </div>

            <button
              type="submit"
              className="btn btn-primary"
              style={{ width: '100%', justifyContent: 'center', marginTop: '0.5rem' }}
              disabled={loading}
            >
              {loading ? '...' : 'עדכן סיסמה'}
            </button>
          </form>
        ) : (
          <div className="alert alert-error">
            הקישור אינו תקף או שפג תוקפו
          </div>
        )}

        <p style={styles.note}>
          <Link to="/forgot-password" style={styles.link}>שליחת קישור חדש</Link>
          {' · '}
          <Link to="/login" style={styles.link}>חזרה לכניסה</Link>
        </p>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  page: { minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'linear-gradient(135deg, #1E3A5F 0%, #2E5D8E 100%)', padding: '1rem' },
  card: { background: '#fff', borderRadius: '16px', padding: '2rem', width: '100%', maxWidth: '420px', boxShadow: '0 8px 40px rgba(0,0,0,0.2)' },
  header: { textAlign: 'center', marginBottom: '1.5rem' },
  title: { fontSize: '1.5rem', color: 'var(--color-primary)', fontWeight: 700, margin: '0.1rem 0' },
  subtitle: { color: 'var(--color-gray)', fontSize: '0.9rem' },
  note: { marginTop: '1rem', fontSize: '0.8rem', color: 'var(--color-gray)', textAlign: 'center' },
  link: { color: 'var(--color-primary)', fontWeight: 600, textDecoration: 'none' },
};
