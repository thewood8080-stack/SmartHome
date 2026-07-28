// נתיבי רשימת קניות
import { Router, Response } from 'express';
import ShoppingItem from '../models/ShoppingItem';
import { requireAuth, AuthRequest } from '../middleware/auth';
import { emitToHousehold } from '../socket';

const router = Router();

// קבלת כל הפריטים
router.get('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const items = await ShoppingItem.find({ householdId: req.householdId })
      .populate('addedBy', 'name photoURL')
      .populate('boughtBy', 'name')
      .sort({ createdAt: -1 });
    res.json(items);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// הוספת פריט
router.post('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { name, quantity, unit, category, urgent } = req.body;
    const item = await ShoppingItem.create({
      name, quantity, unit, category, urgent, householdId: req.householdId,
      addedBy: req.userId,
    });
    const populated = await item.populate('addedBy', 'name photoURL');
    emitToHousehold(req.householdId, 'shopping:created', populated);
    res.status(201).json(populated);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// סימון כנקנה / לא נקנה
router.patch('/:id/bought', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { bought } = req.body;
    const item = await ShoppingItem.findByIdAndUpdate(
      req.params.id,
      {
        bought,
        boughtBy: bought ? req.userId : null,
        boughtAt: bought ? new Date() : null,
      },
      { new: true }
    ).populate('addedBy', 'name photoURL').populate('boughtBy', 'name');
    if (!item) { res.status(404).json({ message: 'פריט לא נמצא' }); return; }
    emitToHousehold(req.householdId, 'shopping:updated', item);
    res.json(item);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// מחיקת פריט
router.delete('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    await ShoppingItem.findByIdAndDelete(req.params.id);
    emitToHousehold(req.householdId, 'shopping:deleted', { id: req.params.id });
    res.json({ message: 'הפריט נמחק' });
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// ניקוי כל הנקנה — לפי בית בלבד
router.delete('/clear/bought', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    await ShoppingItem.deleteMany({ householdId: req.householdId, bought: true });
    emitToHousehold(req.householdId, 'shopping:cleared', {});
    res.json({ message: 'הפריטים שנקנו נמחקו' });
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

export default router;
