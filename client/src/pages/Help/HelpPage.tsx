// עמוד הסבר — SmartHome
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

const sections = [
  {
    icon: '🏠',
    title: 'מה זה SmartHome?',
    content: [
      'SmartHome היא אפליקציה לניהול חכם של הבית המשפחתי.',
      'כל בני המשפחה רואים ומעדכנים הכל בזמן אמת — ממשימות בית ועד ניהול תקציב.',
      'כל משפחה מבודדת לחלוטין — אף אחד מחוץ לבית לא רואה את הנתונים שלכם.',
    ],
  },
  {
    icon: '🚀',
    title: 'איך מתחילים?',
    steps: [
      { num: '1', text: 'אחד ההורים לוחץ "בית חדש" ויוצר חשבון — הופך למנהל/ת.' },
      { num: '2', text: 'הוא מקבל קוד הצטרפות ייחודי (6 ספרות) בדף ⚙️ ניהול.' },
      { num: '3', text: 'שולח את הקוד לבני המשפחה.' },
      { num: '4', text: 'כל אחד נרשם עם "הצטרפות" + הקוד — ממתין לאישור.' },
      { num: '5', text: 'המנהל מאשר כל בן משפחה בדף ניהול.' },
    ],
  },
  {
    icon: '👑',
    title: 'מנהל הבית',
    content: [
      'מנהל = אבא או אמא (עד 2 מנהלים לבית).',
      'המנהל מאשר/חוסם בני משפחה.',
      'המנהל יוצר ומוחק משימות, אירועים ורשומות.',
      'קוד ההצטרפות גלוי רק למנהל בדף ⚙️ ניהול.',
    ],
  },
  {
    icon: '✅',
    title: 'משימות בית',
    content: [
      'צור משימה עם עדיפות: דחוף / רגיל / יכול לחכות.',
      'שייך משימה לבן משפחה ספציפי.',
      'כשמשימה מסומנת כ"בוצע" — הבן משפחה מקבל נקודות.',
      'דחוף = 30 נקודות | רגיל = 20 | יכול לחכות = 10.',
    ],
  },
  {
    icon: '🛒',
    title: 'רשימת קניות',
    content: [
      'כל אחד יכול להוסיף פריטים לרשימה.',
      'סמן ⚡ "דחוף" לפריטים שחייבים לקנות היום.',
      'כשקונים פריט — מסמנים אותו ✓ וכולם רואים מיד.',
      'בסוף הקניות לחצו "נקה שנקנו" לניקוי הרשימה.',
    ],
  },
  {
    icon: '💰',
    title: 'תקציב',
    content: [
      'הוסף הכנסות והוצאות לפי חודש וקטגוריה.',
      'הגרף מציג יתרה — כמה נשאר מהתקציב.',
      'ניתן לסנן לפי חודש ספציפי.',
    ],
  },
  {
    icon: '📅',
    title: 'לוח שנה',
    content: [
      'הוסף אירועים משפחתיים — ימי הולדת, חתונות, אירועים.',
      'אירועים שמתקרבים (עד 7 ימים) מסומנים באדום.',
      'ניתן לצבוע כל אירוע בצבע שונה.',
    ],
  },
  {
    icon: '🎁',
    title: 'מתנות',
    content: [
      'שמור רעיונות למתנות לכל אירוע.',
      'סמן "נקנה" כשקנית — הכנס מה נקנה.',
      'האפליקציה מתריעה 7 ימים לפני האירוע.',
      'אין המלצות סכום — רק רעיונות!',
    ],
  },
  {
    icon: '📦',
    title: 'מלאי הבית',
    content: [
      'עקוב אחר מה יש בבית — מזון, ניקיון, תרופות.',
      'הגדר כמות מינימלית — תקבל התראה כשיורדים מתחתיה.',
      'עדכן כמויות בלחיצה על + / −.',
    ],
  },
  {
    icon: '🏥',
    title: 'תיק רפואי',
    content: [
      'שמור תורים לרופא, תרופות וחיסונים לכל בן משפחה.',
      'פלטר לפי בן משפחה ספציפי.',
      'שמור פרטי רופא ומרפאה לכל רשומה.',
    ],
  },
  {
    icon: '🚗',
    title: 'ניהול רכב',
    content: [
      'עקוב אחר תאריך טסט, ביטוח וטיפול לכל רכב.',
      'התראה אוטומטית חודש לפני תפוגה.',
      'אדום = עבר | צהוב = עוד 30 יום | ירוק = תקין.',
    ],
  },
  {
    icon: '🏆',
    title: 'לוח מובילים',
    content: [
      'כל ביצוע משימה = נקודות אוטומטיות.',
      'ראה מי ביצע הכי הרבה משימות השבוע.',
      'פודיום לשלושת המובילים.',
      'תגים: 🥇🥈🥉 לפי מספר הנקודות.',
    ],
  },
  {
    icon: '📱',
    title: 'הוסף לנייד',
    steps: [
      { num: 'אנדרואיד', text: 'Chrome → 3 נקודות ⋮ → "הוסף למסך הבית"' },
      { num: 'iPhone', text: 'Safari → כפתור שיתוף ↑ → "הוסף למסך הבית"' },
    ],
  },
];

export default function HelpPage() {
  const [open, setOpen] = useState<number | null>(0);
  const navigate = useNavigate();

  return (
    <div style={{ maxWidth: '680px', margin: '0 auto' }}>
      <div style={styles.header}>
        <h1 style={styles.title}>❓ מדריך שימוש</h1>
        <p style={styles.subtitle}>כל מה שצריך לדעת על SmartHome</p>
      </div>

      {sections.map((sec, i) => (
        <div key={i} className="card" style={{ marginBottom: '0.6rem', padding: 0, overflow: 'hidden' }}>
          <button
            onClick={() => setOpen(open === i ? null : i)}
            style={styles.sectionBtn}
          >
            <span style={{ fontSize: '1.3rem' }}>{sec.icon}</span>
            <span style={{ flex: 1, fontWeight: 700, textAlign: 'right' }}>{sec.title}</span>
            <span style={{ color: 'var(--color-gray)', fontSize: '1.1rem' }}>{open === i ? '▲' : '▼'}</span>
          </button>

          {open === i && (
            <div style={styles.sectionBody}>
              {'steps' in sec ? (
                sec.steps?.map((step, j) => (
                  <div key={j} style={styles.step}>
                    <div style={styles.stepNum}>{step.num}</div>
                    <div style={styles.stepText}>{step.text}</div>
                  </div>
                ))
              ) : (
                sec.content?.map((line, j) => (
                  <div key={j} style={styles.bullet}>
                    <span style={{ color: 'var(--color-secondary)', marginLeft: '0.5rem' }}>◆</span>
                    {line}
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      ))}

      <div className="card" style={{ marginTop: '1rem', textAlign: 'center', background: 'var(--color-primary)' }}>
        <p style={{ color: 'rgba(255,255,255,0.8)', fontSize: '0.9rem', marginBottom: '0.75rem' }}>
          מוכן להתחיל? חזור לדשבורד
        </p>
        <button className="btn btn-secondary" onClick={() => navigate('/dashboard')}>
          🏠 חזור לדשבורד
        </button>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  header: { marginBottom: '1.25rem' },
  title: { fontSize: '1.5rem', fontWeight: 700, color: 'var(--color-primary)' },
  subtitle: { color: 'var(--color-gray)', fontSize: '0.9rem', marginTop: '0.25rem' },
  sectionBtn: {
    width: '100%', display: 'flex', alignItems: 'center', gap: '0.75rem',
    padding: '1rem 1.25rem', background: 'none', textAlign: 'right',
    borderBottom: '1px solid var(--color-light)',
  },
  sectionBody: { padding: '0.75rem 1.25rem 1rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' },
  bullet: { fontSize: '0.9rem', display: 'flex', alignItems: 'flex-start', gap: '0.25rem', lineHeight: 1.5 },
  step: { display: 'flex', alignItems: 'flex-start', gap: '0.75rem' },
  stepNum: {
    minWidth: '28px', height: '28px', borderRadius: '50%',
    background: 'var(--color-primary)', color: '#fff',
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    fontWeight: 700, fontSize: '0.82rem', flexShrink: 0,
  },
  stepText: { fontSize: '0.9rem', lineHeight: 1.5, paddingTop: '0.2rem' },
};
