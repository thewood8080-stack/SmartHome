// נתיבי אימות — הרשמה, כניסה, פרופיל
import { Router, Request, Response } from 'express';
import bcrypt from 'bcryptjs';
import jwt from 'jsonwebtoken';
import User from '../models/User';

const router = Router();
const JWT_SECRET = process.env.JWT_SECRET || 'smarthome_secret';

// הרשמה
router.post('/register', async (req: Request, res: Response) => {
  try {
    const { name, email, password } = req.body;

    // בדיקה אם המייל כבר קיים
    const existing = await User.findOne({ email });
    if (existing) {
      res.status(400).json({ message: 'כתובת המייל כבר רשומה' });
      return;
    }

    // הצפנת סיסמה
    const hashed = await bcrypt.hash(password, 10);

    // בדיקה אם זה המשתמש הראשון — יהיה מנהל ומאושר אוטומטית
    const count = await User.countDocuments();
    const isFirst = count === 0;

    const user = await User.create({
      name,
      email,
      password: hashed,
      role: isFirst ? 'admin' : 'member',
      approved: isFirst,
    });

    const token = jwt.sign({ id: user._id }, JWT_SECRET, { expiresIn: '30d' });

    res.status(201).json({
      token,
      user: { id: user._id, name: user.name, email: user.email, role: user.role, approved: user.approved },
    });
  } catch (err) {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// כניסה
router.post('/login', async (req: Request, res: Response) => {
  try {
    const { email, password } = req.body;

    const user = await User.findOne({ email });
    if (!user) {
      res.status(400).json({ message: 'מייל או סיסמה שגויים' });
      return;
    }

    const valid = await bcrypt.compare(password, user.password);
    if (!valid) {
      res.status(400).json({ message: 'מייל או סיסמה שגויים' });
      return;
    }

    const token = jwt.sign({ id: user._id }, JWT_SECRET, { expiresIn: '30d' });

    res.json({
      token,
      user: { id: user._id, name: user.name, email: user.email, role: user.role, approved: user.approved, points: user.points },
    });
  } catch (err) {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// פרופיל משתמש מחובר
router.get('/me', async (req: Request, res: Response) => {
  try {
    const token = req.headers.authorization?.split(' ')[1];
    if (!token) { res.status(401).json({ message: 'לא מורשה' }); return; }

    const decoded = jwt.verify(token, JWT_SECRET) as { id: string };
    const user = await User.findById(decoded.id).select('-password');
    if (!user) { res.status(404).json({ message: 'משתמש לא נמצא' }); return; }

    res.json(user);
  } catch {
    res.status(401).json({ message: 'טוקן לא תקין' });
  }
});

export default router;
