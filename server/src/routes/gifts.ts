// נתיבי מתנות
import { Router, Response } from 'express';
import Gift from '../models/Gift';
import { requireAuth, AuthRequest } from '../middleware/auth';

const router = Router();

// קבלת כל המתנות
router.get('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const gifts = await Gift.find({ householdId: req.householdId })
      .populate('addedBy', 'name')
      .sort({ date: 1 });
    res.json(gifts);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// הוספת מתנה
router.post('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { recipientName, occasion, date, ideas, note } = req.body;
    const gift = await Gift.create({
      recipientName, occasion, date, ideas, note, householdId: req.householdId,
      addedBy: req.userId,
    });
    res.status(201).json(gift);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// עדכון מתנה (רכישה / עדכון כללי)
router.put('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const gift = await Gift.findByIdAndUpdate(req.params.id, req.body, { new: true });
    if (!gift) { res.status(404).json({ message: 'מתנה לא נמצאה' }); return; }
    res.json(gift);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// מחיקת מתנה
router.delete('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    await Gift.findByIdAndDelete(req.params.id);
    res.json({ message: 'המתנה נמחקה' });
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

export default router;
