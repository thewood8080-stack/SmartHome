// חיבור SignalR לצד הלקוח
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

// VITE_API_URL הוא ה-origin בלבד; בפיתוח vite מפרוקסי רק /api, ולכן כתובת ה-Hub מלאה
const HUB_URL = `${import.meta.env.VITE_API_URL || 'http://localhost:5263'}/hubs/smarthome`;

let connection: HubConnection | null = null;

/**
 * מחזיר את החיבור הקיים, ואם אין — יוצר ומתחיל אחד.
 * householdId כבר לא נשלח: השרת משייך את החיבור לקבוצת הבית לפי המשתמש המחובר,
 * כך שאי אפשר לבקש להאזין לבית של מישהו אחר. הפרמטר נשמר כדי לא לשנות את הקוד הקורא.
 */
export const connectSocket = (_householdId?: string | null): HubConnection => {
  if (!connection) {
    connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    // ההאזנות נרשמות לפני שהחיבור עולה, ולכן אין צורך להמתין ל-start
    connection.start().catch(() => {
      // כשל בהתחברות לא אמור להפיל את הדף — הדפים עובדים גם בלי עדכון חי
    });
  }

  return connection;
};

export const disconnectSocket = (): void => {
  if (connection) {
    const active = connection;
    connection = null;
    active.stop().catch(() => {
      // עצירה באמצע התחברות זורקת — אין מה לעשות עם זה
    });
  }
};

export const getSocket = (): HubConnection | null => connection;
