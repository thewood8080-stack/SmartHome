// נתיבי מלאי הבית
import { Router, Response } from 'express';
import Inventory from '../models/Inventory';
import { requireAuth, AuthRequest } from '../middleware/auth';

const router = Router();

// קבלת כל המלאי
router.get('/', requireAuth, async (_req: AuthRequest, res: Response) => {
  try {
    const items = await Inventory.find()
      .populate('addedBy', 'name')
      .sort({ category: 1, name: 1 });
    res.json(items);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// הוספת פריט
router.post('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { name, category, quantity, unit, minQuantity, location, expiryDate, note } = req.body;
    const item = await Inventory.create({
      name, category, quantity, unit, minQuantity, location, expiryDate, note,
      addedBy: req.userId,
    });
    res.status(201).json(item);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// עדכון כמות
router.patch('/:id/quantity', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { quantity } = req.body;
    const item = await Inventory.findByIdAndUpdate(req.params.id, { quantity }, { new: true });
    if (!item) { res.status(404).json({ message: 'פריט לא נמצא' }); return; }
    res.json(item);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// עדכון פריט
router.put('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const item = await Inventory.findByIdAndUpdate(req.params.id, req.body, { new: true });
    if (!item) { res.status(404).json({ message: 'פריט לא נמצא' }); return; }
    res.json(item);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// מחיקת פריט
router.delete('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    await Inventory.findByIdAndDelete(req.params.id);
    res.json({ message: 'הפריט נמחק' });
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

export default router;
