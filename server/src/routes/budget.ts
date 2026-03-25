// נתיבי תקציב
import { Router, Response } from 'express';
import Budget from '../models/Budget';
import { requireAuth, AuthRequest } from '../middleware/auth';

const router = Router();

// קבלת כל הרשומות (עם אפשרות פילטור לפי חודש)
router.get('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { month, year } = req.query;
    const filter: Record<string, unknown> = {};

    if (month && year) {
      const start = new Date(Number(year), Number(month) - 1, 1);
      const end   = new Date(Number(year), Number(month), 0, 23, 59, 59);
      filter.date = { $gte: start, $lte: end };
    }

    const records = await Budget.find(filter)
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
    const { title, amount, type, category, date, note } = req.body;
    const record = await Budget.create({
      title, amount, type, category, date, note,
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
    const record = await Budget.findByIdAndUpdate(req.params.id, req.body, { new: true });
    if (!record) { res.status(404).json({ message: 'רשומה לא נמצאה' }); return; }
    res.json(record);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// מחיקת רשומה
router.delete('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    await Budget.findByIdAndDelete(req.params.id);
    res.json({ message: 'הרשומה נמחקה' });
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

export default router;
