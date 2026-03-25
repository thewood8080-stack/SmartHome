// נתיבי תיק רפואי
import { Router, Response } from 'express';
import Medical from '../models/Medical';
import { requireAuth, AuthRequest } from '../middleware/auth';

const router = Router();

// קבלת רשומות (אפשרות פילטור לפי memberId)
router.get('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const filter = req.query.memberId ? { memberId: req.query.memberId } : {};
    const records = await Medical.find(filter)
      .populate('memberId', 'name photoURL')
      .populate('addedBy', 'name')
      .sort({ date: -1 });
    res.json(records);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// הוספת רשומה
router.post('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { memberId, type, title, date, doctor, clinic, notes, nextAppointment } = req.body;
    const record = await Medical.create({
      memberId, type, title, date, doctor, clinic, notes, nextAppointment,
      addedBy: req.userId,
    });
    res.status(201).json(record);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// עדכון רשומה
router.put('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const record = await Medical.findByIdAndUpdate(req.params.id, req.body, { new: true });
    if (!record) { res.status(404).json({ message: 'רשומה לא נמצאה' }); return; }
    res.json(record);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// מחיקת רשומה
router.delete('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    await Medical.findByIdAndDelete(req.params.id);
    res.json({ message: 'הרשומה נמחקה' });
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

export default router;
