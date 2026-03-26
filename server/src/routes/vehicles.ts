// נתיבי ניהול רכב
import { Router, Response } from 'express';
import Vehicle from '../models/Vehicle';
import { requireAuth, AuthRequest } from '../middleware/auth';

const router = Router();

// קבלת כל הרכבים
router.get('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const vehicles = await Vehicle.find({ householdId: req.householdId })
      .populate('addedBy', 'name')
      .sort({ createdAt: -1 });
    res.json(vehicles);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// הוספת רכב
router.post('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { name, plateNumber, year, lastService, nextService, insurance, test, fuelType, notes } = req.body;
    const vehicle = await Vehicle.create({
      name, plateNumber, year, lastService, nextService, insurance, test, fuelType, notes, householdId: req.householdId,
      addedBy: req.userId,
    });
    res.status(201).json(vehicle);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// עדכון רכב
router.put('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const vehicle = await Vehicle.findByIdAndUpdate(req.params.id, req.body, { new: true });
    if (!vehicle) { res.status(404).json({ message: 'רכב לא נמצא' }); return; }
    res.json(vehicle);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// מחיקת רכב
router.delete('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    await Vehicle.findByIdAndDelete(req.params.id);
    res.json({ message: 'הרכב נמחק' });
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

export default router;
