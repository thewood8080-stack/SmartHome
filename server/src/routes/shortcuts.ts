// נתיבי קיצורי דרך
import { Router, Response } from 'express';
import Shortcut from '../models/Shortcut';
import { requireAuth, AuthRequest } from '../middleware/auth';

const router = Router();

// קבלת כל הקיצורים
router.get('/', requireAuth, async (_req: AuthRequest, res: Response) => {
  try {
    const shortcuts = await Shortcut.find()
      .populate('addedBy', 'name')
      .sort({ order: 1 });
    res.json(shortcuts);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// הוספת קיצור
router.post('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { label, url, icon, color, order } = req.body;
    const shortcut = await Shortcut.create({
      label, url, icon, color, order,
      addedBy: req.userId,
    });
    res.status(201).json(shortcut);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// עדכון קיצור
router.put('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const shortcut = await Shortcut.findByIdAndUpdate(req.params.id, req.body, { new: true });
    if (!shortcut) { res.status(404).json({ message: 'קיצור לא נמצא' }); return; }
    res.json(shortcut);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// מחיקת קיצור
router.delete('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    await Shortcut.findByIdAndDelete(req.params.id);
    res.json({ message: 'הקיצור נמחק' });
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

export default router;
