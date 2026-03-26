// נתיבי אימות — הרשמה, כניסה, פרופיל
import { Router, Request, Response } from 'express';
import bcrypt from 'bcryptjs';
import jwt from 'jsonwebtoken';
import mongoose from 'mongoose';
import User from '../models/User';
import Household from '../models/Household';
import { requireAuth, AuthRequest } from '../middleware/auth';

const router = Router();
const JWT_SECRET = process.env.JWT_SECRET || 'smarthome_secret';

const generateCode = () => Math.random().toString(36).substring(2, 8).toUpperCase();

// הרשמה
router.post('/register', async (req: Request, res: Response) => {
  try {
    const { name, email, password, householdName, inviteCode } = req.body;

    if (await User.findOne({ email })) {
      res.status(400).json({ message: 'כתובת המייל כבר רשומה' });
      return;
    }

    const hashed = await bcrypt.hash(password, 10);
    let household;
    let role: 'admin' | 'member' = 'member';
    let approved = false;

    if (inviteCode) {
      // הצטרפות לבית קיים
      household = await Household.findOne({ inviteCode: inviteCode.toUpperCase() });
      if (!household) {
        res.status(400).json({ message: 'קוד הצטרפות שגוי' });
        return;
      }
    } else {
      // יצירת בית חדש
      if (!householdName) {
        res.status(400).json({ message: 'נדרש שם הבית' });
        return;
      }
      let code = generateCode();
      while (await Household.findOne({ inviteCode: code })) { code = generateCode(); }
      household = await Household.create({
        name: householdName,
        inviteCode: code,
        createdBy: new mongoose.Types.ObjectId(),
      });
      role = 'admin';
      approved = true;
    }

    const user = await User.create({
      name, email, password: hashed, role, approved,
      householdId: household._id,
    });

    if (!inviteCode) {
      await Household.findByIdAndUpdate(household._id, { createdBy: user._id });
    }

    const token = jwt.sign({ id: user._id }, JWT_SECRET, { expiresIn: '30d' });

    res.status(201).json({
      token,
      user: { id: user._id, name: user.name, email: user.email, role: user.role, approved: user.approved, points: user.points },
      household: { name: household.name, inviteCode: household.inviteCode },
    });
  } catch (err) {
    console.error(err);
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// כניסה
router.post('/login', async (req: Request, res: Response) => {
  try {
    const { email, password } = req.body;
    const user = await User.findOne({ email });
    if (!user) { res.status(400).json({ message: 'מייל או סיסמה שגויים' }); return; }

    const valid = await bcrypt.compare(password, user.password);
    if (!valid) { res.status(400).json({ message: 'מייל או סיסמה שגויים' }); return; }

    if (!user.approved) { res.status(403).json({ message: 'ממתין לאישור מנהל הבית' }); return; }

    const household = await Household.findById(user.householdId);
    const token = jwt.sign({ id: user._id }, JWT_SECRET, { expiresIn: '30d' });

    res.json({
      token,
      user: { id: user._id, name: user.name, email: user.email, role: user.role, approved: user.approved, points: user.points },
      household: household ? { name: household.name, inviteCode: household.inviteCode } : null,
    });
  } catch {
    res.status(500).json({ message: 'שגיאת שרת' });
  }
});

// פרופיל
router.get('/me', requireAuth, async (req: AuthRequest, res: Response) => {
  try {
    const user = await User.findById(req.userId).select('-password');
    if (!user) { res.status(404).json({ message: 'משתמש לא נמצא' }); return; }
    const household = await Household.findById(user.householdId);
    res.json({ ...user.toObject(), household });
  } catch {
    res.status(401).json({ message: 'טוקן לא תקין' });
  }
});

export default router;
