// שרת ראשי — SmartHome
import express from 'express';
import { createServer } from 'http';
import cors from 'cors';
import dotenv from 'dotenv';
import mongoose from 'mongoose';
import { initSocket } from './socket';
import authRoutes from './routes/auth';
import taskRoutes from './routes/tasks';
import shoppingRoutes from './routes/shopping';
import budgetRoutes from './routes/budget';
import eventRoutes from './routes/events';
import giftRoutes from './routes/gifts';
import medicalRoutes from './routes/medical';
import vehicleRoutes from './routes/vehicles';
import inventoryRoutes from './routes/inventory';
import shortcutRoutes from './routes/shortcuts';
import userRoutes from './routes/users';

dotenv.config();

const app = express();
const httpServer = createServer(app);
const PORT = process.env.PORT || 5000;

// אתחול Socket.io
initSocket(httpServer);

// Middleware
app.use(cors({ origin: process.env.CLIENT_URL || 'http://localhost:5173', credentials: true }));
app.use(express.json());

// חיבור למסד הנתונים
mongoose
  .connect(process.env.MONGODB_URI || '')
  .then(() => console.log('✅ מחובר ל-MongoDB'))
  .catch((err) => console.error('❌ שגיאת חיבור MongoDB:', err));

// נתיבים
app.use('/api/auth',      authRoutes);
app.use('/api/tasks',     taskRoutes);
app.use('/api/shopping',  shoppingRoutes);
app.use('/api/budget',    budgetRoutes);
app.use('/api/events',    eventRoutes);
app.use('/api/gifts',     giftRoutes);
app.use('/api/medical',   medicalRoutes);
app.use('/api/vehicles',  vehicleRoutes);
app.use('/api/inventory', inventoryRoutes);
app.use('/api/shortcuts', shortcutRoutes);
app.use('/api/users',     userRoutes);

app.get('/api/health', (_req, res) => {
  res.json({ status: 'ok', message: 'SmartHome Server פעיל' });
});

httpServer.listen(PORT, () => {
  console.log(`🚀 השרת פועל על פורט ${PORT}`);
});
