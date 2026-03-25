// נתיבי משימות
import { Router, Response } from 'express';
import Task from '../models/Task';
import User from '../models/User';
import { requireAuth, AuthRequest } from '../middleware/auth';
import { emitToAll } from '../socket';

const router = Router();

// קבלת כל המשימות
router.get('/', requireAuth, async (_req: AuthRequest, res: Response) => {
  try {
    const tasks = await Task.find()
      .populate('assignedTo', 'name photoURL')
      .populate('createdBy', 'name')
      .sort({ createdAt: -1 });
    res.json(tasks);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// יצירת משימה
router.post('/', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { title, description, assignedTo, dueDate, priority, recurring } = req.body;

    // חישוב נקודות לפי עדיפות
    const pointsMap: Record<string, number> = { high: 30, medium: 20, low: 10 };
    const points = pointsMap[priority] || 20;

    const task = await Task.create({
      title, description, assignedTo, dueDate, priority, recurring,
      points,
      createdBy: req.userId,
    });

    const populated = await task.populate(['assignedTo', 'createdBy']);
    emitToAll('task:created', populated);
    res.status(201).json(populated);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// עדכון סטטוס משימה
router.patch('/:id/status', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const { status } = req.body;
    const task = await Task.findById(req.params.id);
    if (!task) { res.status(404).json({ message: 'משימה לא נמצאה' }); return; }

    const wasNotDone = task.status !== 'done';
    task.status = status;
    if (status === 'done') {
      task.completedAt = new Date();

      // הוספת נקודות למשתמש שביצע
      if (wasNotDone && req.userId) {
        await User.findByIdAndUpdate(req.userId, { $inc: { points: task.points } });
      }
    }

    await task.save();
    emitToAll('task:updated', task);
    res.json(task);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// עדכון משימה
router.put('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const task = await Task.findByIdAndUpdate(req.params.id, req.body, { new: true })
      .populate('assignedTo', 'name photoURL')
      .populate('createdBy', 'name');
    if (!task) { res.status(404).json({ message: 'משימה לא נמצאה' }); return; }
    emitToAll('task:updated', task);
    res.json(task);
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// מחיקת משימה
router.delete('/:id', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    await Task.findByIdAndDelete(req.params.id);
    emitToAll('task:deleted', { id: req.params.id });
    res.json({ message: 'המשימה נמחקה' });
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

export default router;
