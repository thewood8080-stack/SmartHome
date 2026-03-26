// נתיבי לוח שנה ואירועים
import { Router, Response } from 'express';
import Event from '../models/Event';
import { requireAuth, AuthRequest } from '../middleware/auth';

const router = Router();

// קבלת כל האירועים
router.get('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const events = await Event.find({ householdId: req.householdId })
      .populate('createdBy', 'name photoURL')
      .populate('participants', 'name photoURL')
      .sort({ date: 1 });
    res.json(events);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// יצירת אירוע
router.post('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { title, description, date, endDate, allDay, color, participants, reminder } = req.body;
    const event = await Event.create({
      title, description, date, endDate, allDay, color, participants, reminder, householdId: req.householdId,
      createdBy: req.userId,
    });
    const populated = await event.populate(['createdBy', 'participants']);
    res.status(201).json(populated);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// עדכון אירוע
router.put('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const event = await Event.findByIdAndUpdate(req.params.id, req.body, { new: true })
      .populate('createdBy', 'name photoURL')
      .populate('participants', 'name photoURL');
    if (!event) { res.status(404).json({ message: 'אירוע לא נמצא' }); return; }
    res.json(event);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// מחיקת אירוע
router.delete('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    await Event.findByIdAndDelete(req.params.id);
    res.json({ message: 'האירוע נמחק' });
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

export default router;
