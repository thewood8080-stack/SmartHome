import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    // Service Worker — בלעדיו "הוסף למסך הבית" יוצר קיצור דרך ולא אפליקציה.
    VitePWA({
      // רישום ה-SW מוזרק אוטומטית ל-index.html, ולכן אין קריאה ידנית ב-main.tsx.
      injectRegister: 'auto',
      // גרסה חדשה מחליפה את הישנה בלי שהמשתמש יצטרך לסגור את האפליקציה.
      registerType: 'autoUpdate',
      // המניפסט נשאר ב-public/manifest.json כמקור יחיד, ו-index.html כבר מקשר אליו.
      // בלי false הפלאגין היה מייצר מניפסט שני ומתחרה.
      manifest: false,
      workbox: {
        globPatterns: ['**/*.{js,css,html,png,svg,woff2}'],
        // האפליקציה היא SPA — כל נתיב פנימי מוגש מ-index.html שבמטמון,
        // אחרת רענון על /medical בנייד היה מחזיר 404 במצב offline.
        navigateFallback: '/index.html',
        // קריאות ה-API הן ל-origin אחר ולעולם לא נכנסות ל-precache,
        // כדי שלא יוצג מידע משק בית ישן מהמטמון.
        navigateFallbackDenylist: [/^\/api/],
      },
      devOptions: {
        // ה-SW לא רץ ב-dev כדי שלא ישרת קבצים ישנים מהמטמון בזמן פיתוח.
        enabled: false,
      },
    }),
  ],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5263',
        changeOrigin: true,
      },
    },
  },
})
