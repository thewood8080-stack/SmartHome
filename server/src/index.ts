// שרת ראשי — SmartHome
import express from 'express';
import cors from 'cors';
import dotenv from 'dotenv';
import mongoose from 'mongoose';
import authRoutes from './routes/auth';

dotenv.config();

const app = express();
const PORT = process.env.PORT || 5000;

// Middleware
app.use(cors({ origin: process.env.CLIENT_URL || 'http://localhost:5173', credentials: true }));
app.use(express.json());

// חיבור למסד הנתונים
mongoose
  .connect(process.env.MONGODB_URI || '')
  .then(() => console.log('✅ מחובר ל-MongoDB'))
  .catch((err) => console.error('❌ שגיאת חיבור MongoDB:', err));

// נתיבים
app.use('/api/auth', authRoutes);

app.get('/api/health', (_req, res) => {
  res.json({ status: 'ok', message: 'SmartHome Server פעיל' });
});

app.listen(PORT, () => {
  console.log(`🚀 השרת פועל על פורט ${PORT}`);
});
