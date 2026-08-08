# דוח מצב — SmartHome

**תאריך הבדיקה:** 29/07/2026
**מטרת הדוח:** בסיס להחלטה על הסבת השרת מ-Node/Express ל-ASP.NET Core Web API
**אופי הבדיקה:** קריאה בלבד — לא בוצע שינוי, יצירה, מחיקה או פעולת git שמשנה מצב.

> ⚠️ **הערה:** באמצע הבדיקה (בין שתי בדיקות `git status` רצופות) בוצע commit `4a10b2e`
> על ידי תהליך חיצוני — כנראה ה-Git integration של Visual Studio / VS Code שרץ במקביל.
> הבדיקה עצמה הריצה רק פקודות קריאה (`status`, `log`, `remote -v`, `fetch --dry-run`, `rev-parse`).

---

## 1. GIT

| פריט | מצב |
|---|---|
| Branch נוכחי | `main` |
| Remote | `origin` → `https://github.com/thewood8080-stack/SmartHome.git` (fetch + push) |
| שינויים לא מקומיטים | אין — עץ העבודה נקי |
| Untracked | `.vs/` בלבד (metadata של Visual Studio, לא חלק מהריפו) |
| סנכרון מול remote | מסונכרן מלא — 0 ahead, 0 behind |

**היסטוריית commits — קיימים 11 בסך הכל (לא 15):**

```
4a10b2e עדכון מודולים: Admin, Auth, Gamification, socket, authStore + העברת CLAUDE.md לשורש
ed8234d הוסף עמוד מדריך שימוש
38aa060 הוסף מערכת בתים — כל משפחה מבודדת לחלוטין
a998b7a התאמה למובייל — bottom nav + responsive layout
d0fd777 השאר רק אייקון אחד — icon-512.png
b6e15b7 הוסף אייקון אפליקציה ו-PWA manifest
e9f10d0 הוסף .env.production לקליינט עם URL הייצור
dd3e8ea שלב 1-4: מודלים, routes, Socket.io, וכל מסכי UI
5f24a37 הוספת מערכת אימות — הרשמה וכניסה עם מייל וסיסמה
d35c87b התחלת פרויקט SmartHome — מבנה בסיסי
fca1998 Initial commit
```

---

## 2. SERVER

### מבנה תיקיות

```
server/src/
├── index.ts                  (63 שורות)  — הרכבת Express + CORS + Mongo + רישום routes
├── socket.ts                 (42 שורות)  — אתחול Socket.IO + חדרים לפי בית
├── middleware/
│   └── auth.ts               (26 שורות)  — אימות JWT + טעינת householdId
├── models/                   (11 מודלים, ~284 שורות)
│   Budget, Event, Gift, Household, Inventory, Medical,
│   ShoppingItem, Shortcut, Task, User, Vehicle
└── routes/                   (11 routers, ~779 שורות)
    auth, budget, events, gifts, inventory, medical,
    shopping, shortcuts, tasks, users, vehicles
```

**סה"כ שורות קוד ב-`server/src`: 1,194**

### Endpoints — 49 בפועל

| Base | Method + Path | הערה |
|---|---|---|
| `/api/auth` | `POST /register` | יצירת בית חדש או הצטרפות עם inviteCode |
| | `POST /login` | |
| | `GET /me` | מוגן |
| `/api/tasks` | `GET /` · `POST /` · `PATCH /:id/status` · `PUT /:id` · `DELETE /:id` | ✅ real-time |
| `/api/shopping` | `GET /` · `POST /` · `PATCH /:id/bought` · `DELETE /:id` · `DELETE /clear/bought` | ✅ real-time |
| `/api/budget` | `GET /` (פילטר `month`/`year`) · `POST /` · `PUT /:id` · `DELETE /:id` | |
| `/api/events` | `GET /` · `POST /` · `PUT /:id` · `DELETE /:id` | |
| `/api/gifts` | `GET /` · `POST /` · `PUT /:id` · `DELETE /:id` | |
| `/api/medical` | `GET /` (פילטר `memberId`) · `POST /` · `PUT /:id` · `DELETE /:id` | |
| `/api/vehicles` | `GET /` · `POST /` · `PUT /:id` · `DELETE /:id` | |
| `/api/inventory` | `GET /` · `POST /` · `PATCH /:id/quantity` · `PUT /:id` · `DELETE /:id` | |
| `/api/shortcuts` | `GET /` · `POST /` · `PUT /:id` · `DELETE /:id` | |
| `/api/users` | `GET /` · `PATCH /:id/approve` · `PATCH /:id/role` · `GET /leaderboard` · `POST /reset-points` · `DELETE /:id` | |
| כללי | `GET /api/health` | |

כל ה-endpoints (מלבד register/login/health) מוגנים ב-`requireAuth`.

### מודלים MongoDB (Mongoose)

כולם עם `{ timestamps: true }`, וכולם מקושרים ל-`householdId` לבידוד בין משפחות.

| מודל | שדות |
|---|---|
| **User** | `name`, `email` (unique, lowercase), `password` (hashed), `role` (`admin`\|`member`), `photoURL?`, `points` (0), `approved` (false), `householdId` → Household |
| **Household** | `name`, `inviteCode` (unique, uppercase), `createdBy` → User |
| **Task** | `title`, `description?`, `assignedTo?` → User, `createdBy` → User, `householdId`, `dueDate?`, `priority` (`low`\|`medium`\|`high`), `status` (`todo`\|`inprogress`\|`done`), `points` (10), `recurring` (`none`\|`daily`\|`weekly`\|`monthly`), `completedAt?` |
| **ShoppingItem** | `name`, `quantity` (1), `unit?`, `category?`, `addedBy` → User, `bought` (false), `boughtBy?` → User, `boughtAt?`, `urgent` (false) |
| **Budget** | `title`, `amount`, `type` (`income`\|`expense`), `category`, `date`, `note?`, `addedBy` → User |
| **Event** | `title`, `description?`, `date`, `endDate?`, `allDay` (false), `color` (`#5C6BC0`), `createdBy` → User, `householdId`, `participants[]` → User, `reminder?` (דקות לפני) |
| **Gift** | `recipientName`, `occasion`, `date`, `ideas[]`, `budget?`, `purchased` (false), `purchasedItem?`, `purchasedPrice?`, `note?`, `addedBy` → User |
| **Inventory** | `name`, `category`, `quantity` (0), `unit?`, `minQuantity?` (0), `location?`, `expiryDate?`, `note?`, `addedBy` → User |
| **Medical** | `memberId` → User, `type` (`appointment`\|`medication`\|`vaccine`\|`note`), `title`, `date`, `doctor?`, `clinic?`, `notes?`, `nextAppointment?`, `addedBy` → User |
| **Vehicle** | `name`, `plateNumber?`, `year?`, `lastService?`, `nextService?`, `insurance?`, `test?` (טסט), `fuelType?`, `notes?`, `addedBy` → User |
| **Shortcut** | `label`, `url`, `icon?`, `color` (`#5C6BC0`), `order` (0), `addedBy` → User |

### אימות — **לא Google OAuth**

המימוש בפועל **סוטה מהמפרט ב-CLAUDE.md**:

- **בפועל:** Email + Password עם `bcryptjs` (hash, salt=10) + **JWT** (`jsonwebtoken`, תוקף 30 יום).
- `requireAuth` מאמת `Authorization: Bearer <token>`, מוודא `user.approved === true`, ומזריק `userId` + `householdId` ל-request.
- **Google OAuth 2.0 — לא ממומש כלל.** התלות `@react-oauth/google` קיימת ב-`client/package.json` אך **אינה בשימוש בשום קובץ**.
- `JWT_SECRET` נופל ל-default קשיח (`'smarthome_secret'`) אם אין env var — נקודה לתשומת לב אבטחתית.

### Real-time — Socket.IO מחובר אך **חלקי**

`socket.ts` מאתחל Socket.IO עם חדר per-household. אירועים:

- **חיבור:** `connection` → `join:household` (הצטרפות לחדר) → `disconnect`
- **שידור:** `emitToHousehold(householdId, event, data)`

**רק 2 מתוך 11 המודולים משדרים אירועים בפועל:**

| מודול | אירועים |
|---|---|
| `tasks` | `task:created`, `task:updated`, `task:deleted` |
| `shopping` | `shopping:created`, `shopping:updated`, `shopping:deleted`, `shopping:cleared` |

**לא משדרים real-time:** `budget`, `events`, `gifts`, `medical`, `vehicles`, `inventory`, `shortcuts`, `users`.
עדכון של בן משפחה אחד באחד המודולים האלה **לא יופיע אצל האחרים** עד רענון/ניווט מחדש —
סטייה מ"חוק הברזל" שמוגדר ב-CLAUDE.md.

---

## 3. CLIENT

**סה"כ שורות קוד ב-`client/src`: 2,659**

### 14 תיקיות `src/pages` — סטטוס מפורט

| # | תיקייה | סטטוס | פירוט |
|---|---|---|---|
| 1 | **Admin** | ✅ מימוש מלא | אישור/חסימת משתמשים, שינוי הרשאות, מחיקה, הצגת+שיתוף קוד הצטרפות |
| 2 | **Auth** | ✅ מימוש מלא | 3 מצבים: כניסה / בית חדש / הצטרפות עם קוד; תמיכה ב-`?join=CODE` מה-URL |
| 3 | **Budget** | ✅ מימוש מלא | CRUD, בורר חודש, סיכומי הכנסות/הוצאות/יתרה, 9 קטגוריות |
| 4 | **Calendar** | ✅ מימוש מלא | CRUD אירועים, הפרדה קרובים/עברו, ספירת ימים, צבע לאירוע |
| 5 | **Dashboard** | ✅ מימוש מלא | 3 כרטיסי סטטיסטיקה, 8 קיצורים למודולים, top-3 מובילים |
| 6 | **Events** | ❌ **תיקייה ריקה לחלוטין** | אין בה אף קובץ. הפונקציונליות ממומשת בפועל ב-`Calendar/CalendarPage.tsx` |
| 7 | **Gamification** | ✅ מימוש מלא | פודיום 3 מקומות, רשימה מלאה, 4 תגים לפי נקודות, איפוס נקודות למנהל |
| 8 | **Gifts** | ✅ מימוש מלא | CRUD, רעיונות כתגיות, סימון "נקנה", התראת 7 ימים. **ללא המלצות סכום** ✔ |
| 9 | **Help** | ✅ מימוש מלא | 13 סעיפי accordion — מדריך סטטי, ללא קריאות API (מטבעו) |
| 10 | **Inventory** | ✅ מימוש מלא | CRUD, חיפוש, קיבוץ לפי קטגוריה, +/− כמות, התראת מלאי נמוך |
| 11 | **Medical** | ✅ מימוש מלא | CRUD, פילטר לפי בן משפחה, 4 סוגי רשומה |
| 12 | **Shopping** | ✅ מימוש מלא | CRUD, סימון נקנה, ניקוי שנקנו, **+ real-time מלא** |
| 13 | **Tasks** | ✅ מימוש מלא | CRUD, שיוך, עדיפויות+נקודות, פילטרים, **+ real-time מלא** |
| 14 | **Vehicle** | ✅ מימוש מלא | CRUD, תגי סטטוס צבעוניים לטסט/ביטוח/טיפול |

**סיכום: 13/14 עם מימוש אמיתי ומחובר ל-API. `Events` היא שריד — תיקייה ריקה, לא placeholder עם קוד.**

### services/

| קובץ | תוכן |
|---|---|
| `api.ts` | instance של **axios** (`baseURL` מ-`VITE_API_URL` או `/api`) + request interceptor שמזריק `Authorization: Bearer <token>` מ-localStorage (`smarthome-auth`) |
| `socket.ts` | עטיפה דקה מעל **socket.io-client** — `connectSocket(householdId)` / `disconnectSocket()` / `getSocket()`, singleton |

### store/

קובץ אחד בלבד — `authStore.ts`:
- **Zustand** + middleware `persist` (localStorage, key `smarthome-auth`)
- מחזיק: `user`, `token`, `household`; פעולות: `setAuth`, `updateUser`, `logout`, `isAdmin()`

**אין stores נוספים.** כל דף אחר מנהל state מקומי ב-`useState` וקורא ל-API ישירות מתוך הקומפוננטה.
זו סטייה מהכלל "אין לוגיקה עסקית בקומפוננטות — רק ב-hooks ו-services".
ה-hook היחיד שקיים הוא `hooks/useSocket.ts` (`useSocket` + `useSocketEvent<T>`).

### PWA — חלקי

| פריט | מצב |
|---|---|
| `public/manifest.json` | ✅ תקין ומלא — `name`, `short_name`, `start_url`, `display: standalone`, `theme_color`, `background_color`, `lang: he`, `dir: rtl`, `orientation: portrait` |
| אייקונים | ⚠️ **גודל אחד בלבד** — `icon-512.png` (`any maskable`). חסר 192x192 שנדרש לחלק מהפלטפורמות |
| **Service Worker** | ❌ **לא קיים כלל** — אין קובץ SW, ואין `vite-plugin-pwa` בתלויות |

**מסקנה:** יש manifest אך **אין PWA טכני** — אין offline, אין caching, אין install-prompt אוטומטי.
"הוסף למסך הבית" הידני (כמתואר ב-HelpPage) עדיין עובד, אך זה bookmark ולא PWA מלא.
זהו חסם ממשי ליעד "פרסום ב-Google Play Store" דרך TWA.

### Stack בפועל (`client/package.json`)

`react@19` · `react-dom@19` · `react-router-dom@7` · `axios@1.7` · `zustand@5` ·
`socket.io-client@4.8` · `dayjs` · `react-hot-toast` · `@react-oauth/google` (**לא בשימוש**)
Build: `vite@6` + `typescript@5.7`

---

## 4. הערכה

### מה באמת מיושם ועובד

| תחום | מצב |
|---|---|
| 11 מודולי CRUD מקצה לקצה (server ↔ MongoDB ↔ client) | ✅ עובדים |
| בידוד בין בתים (`householdId` בכל route) | ✅ ממומש עקבי |
| אימות + הרשאות (admin/member, approve flow) | ✅ עובד (אך לא Google OAuth) |
| גמיפיקציה — נקודות אוטומטיות לפי עדיפות (30/20/10) | ✅ תואם מפרט |
| מודול מתנות — ללא המלצות סכום | ✅ תואם מפרט |
| RTL מלא + פלטת צבעים | ✅ תואם מפרט |
| TypeScript strict, ללא `any` | ✅ לא נמצא `any` |

### פערים מול המפרט ב-CLAUDE.md

| # | פער | חומרה |
|---|---|---|
| 1 | **מודול 12 (התראות) — לא קיים.** אין notification model, אין scheduler/cron, אין push/email. קיימות רק אינדיקציות ויזואליות inline (מלאי נמוך, ימים לטסט, "בעוד X ימים") | 🔴 גבוהה |
| 2 | **Real-time רק ב-2/11 מודולים** — סטייה מ"חוק הברזל" | 🔴 גבוהה |
| 3 | **Google OAuth לא ממומש** — הוחלף ב-email+password | 🟡 בינונית |
| 4 | **אין Service Worker** — חוסם TWA ל-Play Store | 🟡 בינונית |
| 5 | "עד 2 מנהלים לבית" — לא נאכף בקוד | 🟢 נמוכה |
| 6 | "איפוס נקודות שבועי אוטומטי" — קיים רק כפעולה ידנית | 🟢 נמוכה |
| 7 | תיקיית `Events` ריקה | 🟢 נמוכה |
| 8 | אין `utils/strings.ts` — טקסטים מפוזרים בקומפוננטות | 🟢 נמוכה |
| 9 | `JWT_SECRET` עם fallback קשיח | 🟡 בינונית |
| 10 | לוגיקה עסקית בתוך קומפוננטות (בניגוד לכלל) | 🟢 נמוכה |

### **הערכה כוללת: ~65%–70% מהמפרט ממומש בפועל**

ה-CRUD המרכזי (11/12 מודולים) בריא, עקבי ועובד. הקוד קומפקטי ולא מנופח —
סה"כ **~3,853 שורות** (1,194 server + 2,659 client).

---

### מה ייאבד אם נזרוק את `server/` ונכתוב מחדש ב-ASP.NET Core Web API

| מה נאבד | הערכת מאמץ שכתוב |
|---|---|
| 1,194 שורות לוגיקת CRUD עובדת ובדוקה | **נמוך** — רובו קוד חזרתי ומכני (Create/Read/Update/Delete + populate + household-scoping). מיפוי ישיר ל-Controllers |
| 11 Mongoose schemas | **נמוך** — declarative ופשוט להעברה ל-`MongoDB.Driver` ב-.NET |
| לוגיקת בידוד בין בתים | **בינוני — קריטי!** חייב שכפול קפדני בכל endpoint. באג כאן = דליפת נתונים בין משפחות |
| שכבת Socket.IO | **בינוני** — שכתוב מלא ל-SignalR (פרוטוקול שונה לחלוטין) |
| JWT auth + bcrypt | **נמוך** — ל-.NET יש תשתית מובנית מצוינת (`Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net`) |
| DB migrations | **אפס** — אין. MongoDB schema-less |

> **הנקודה הקריטית:** ה-**contract** בין client לשרת (paths + צורת ה-JSON) הוא מה שקובע
> את היקף העבודה בצד ה-client. אם ה-ASP.NET API ישכפל **בדיוק** את אותם 49 endpoints
> ואת אותן צורות JSON (למשל `{ token, user: { id, name, email, role, approved, points }, household: {...} }`)
> — ה-client כמעט לא יזדקק לשינוי כלל.

### היקף העבודה ב-`client/`

| משימה | היקף | פירוט |
|---|---|---|
| **Express → Web API** | **מזערי** | כל התקשורת עוברת דרך `services/api.ts` (20 שורות). אם ה-paths וה-JSON נשמרים — שינוי base URL בלבד. `Bearer` JWT כבר תואם |
| **Socket.IO → SignalR** | **מוגבל — ימי עבודה, לא שבועות** | כל השימוש מרוכז ב-2 קבצים (`services/socket.ts` + `hooks/useSocket.ts`, ~50 שורות) + קריאות `useSocketEvent` ב-2 קומפוננטות בלבד (Tasks, Shopping). מעבר ל-`@microsoft/signalr` |
| שינוי שמות שדות (אם ה-.NET יחזיר `id` במקום `_id`, PascalCase וכו') | **בינוני אם לא נזהרים** | ניתן למנוע לחלוטין ע"י הגדרת `JsonNamingPolicy.CamelCase` + מיפוי `_id` ב-DTOs |

---

## סיכום והמלצה

### ✅ **לשמר את ה-client**

הוא **מנותק היטב** מהשרת — שכבת axios דקה אחת (20 שורות) + wrapper socket אחד (27 שורות).
13/14 הדפים ממומשים במלואם, עקביים ב-RTL ובפלטת הצבעים, ובריאים מבחינת TypeScript.
שכתוב שלו מאפס יהיה **בזבוז של ~2,650 שורות קוד עובד**.

### ⚖️ **את ה-server — שכתוב ממוקד, לא "התחלה מאפס"**

1,194 שורות ה-CRUD הן **מכניות ופשוטות לשכפול** ב-ASP.NET Core. אבל שני דברים דורשים
תשומת לב אמיתית, ואם הם נעשים נכון — המעבר לגיטימי וממוקד ולא "פרויקט חדש":

1. **לשמר בדיוק את ה-JSON contracts** — camelCase, `_id`/`id`, מבנה תגובות הauth.
   זה מה שמאפשר לא לגעת ב-client כמעט בכלל.
2. **לשכתב Socket.IO → SignalR בקפידה** — כולל שימור שמות האירועים
   (`task:created`, `shopping:updated` וכו') ומודל החדרים per-household.

### 💡 שיקול נוסף לפני ההחלטה

לפני ההסבה כדאי לשקול האם היא מטפלת בפערים האמיתיים. **שלושת הפערים הגדולים
(התראות, real-time חלקי, Service Worker) אינם קשורים לשפת השרת** — הם יידרשו
עבודה זהה בין אם השרת נשאר Node ובין אם עובר ל-.NET.

היתרון האמיתי של ASP.NET Core כאן הוא בתחומים כמו: BackgroundService מובנה
למערכת ההתראות (מודול 12), typing חזק יותר בין שכבות, וכלי deployment/monitoring
בשלים יותר. אם אלו הסיבות — המעבר מוצדק. אם הסיבה היא רק העדפת שפה,
כדאי לשקול לסגור קודם את הפערים על התשתית הקיימת.
