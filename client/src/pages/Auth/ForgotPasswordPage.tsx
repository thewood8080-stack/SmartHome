// דף שכחתי סיסמה — שליחת מייל עם קישור לאיפוס
import { useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../../services/api';

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [sent, setSent] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await api.post('/auth/forgot-password', { email });
      // גם בממשק לא מבדילים בין כתובת רשומה ללא רשומה — אותה הודעה תמיד.
      setSent(true);
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
          <h1 style={styles.title}>שכחת סיסמה?</h1>
          <p style={styles.subtitle}>נשלח אליך קישור לאיפוס הסיסמה</p>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        {sent ? (
          <>
            <div className="alert alert-success">
              אם הכתובת קיימת במערכת, נשלח אליה מייל לאיפוס סיסמה
            </div>
            <p style={styles.note}>הקישור במייל תקף ל-30 דקות. בדוק גם בתיקיית הספאם.</p>
          </>
        ) : (
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>כתובת מייל</label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                placeholder="example@email.com"
              />
            </div>

            <button
              type="submit"
              className="btn btn-primary"
              style={{ width: '100%', justifyContent: 'center', marginTop: '0.5rem' }}
              disabled={loading}
            >
              {loading ? '...' : 'שלח קישור לאיפוס'}
            </button>
          </form>
        )}

        <p style={styles.note}>
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
