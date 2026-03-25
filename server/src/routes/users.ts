// נתיבי ניהול משתמשים (למנהל בלבד)
import { Router, Response } from 'express';
import User from '../models/User';
import { requireAuth, AuthRequest } from '../middleware/auth';

const router = Router();

// קבלת כל המשתמשים
router.get('/', requireAuth, async (_req: AuthRequest, res: Response) => {
  try {
    const users = await User.find().select('-password').sort({ createdAt: 1 });
    res.json(users);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// אישור / ביטול אישור משתמש
router.patch('/:id/approve', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { approved } = req.body;
    const user = await User.findByIdAndUpdate(req.params.id, { approved }, { new: true }).select('-password');
    if (!user) { res.status(404).json({ message: 'משתמש לא נמצא' }); return; }
    res.json(user);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// שינוי תפקיד
router.patch('/:id/role', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { role } = req.body;
    const user = await User.findByIdAndUpdate(req.params.id, { role }, { new: true }).select('-password');
    if (!user) { res.status(404).json({ message: 'משתמש לא נמצא' }); return; }
    res.json(user);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// לוח מובילים — נקודות
router.get('/leaderboard', requireAuth, async (_req: AuthRequest, res: Response) => {
  try {
    const users = await User.find({ approved: true })
      .select('name photoURL points role')
      .sort({ points: -1 });
    res.json(users);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// מחיקת משתמש
router.delete('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    await User.findByIdAndDelete(req.params.id);
    res.json({ message: 'המשתמש נמחק' });
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

export default router;
