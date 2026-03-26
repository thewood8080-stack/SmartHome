// middleware לאימות JWT + householdId
import { Request, Response, NextFunction } from 'express';
import jwt from 'jsonwebtoken';
import User from '../models/User';

const JWT_SECRET = process.env.JWT_SECRET || 'smarthome_secret';

export interface AuthRequest extends Request {
  userId?: string;
  householdId?: string;
}

export const requireAuth = async (req: AuthRequest, res: Response, next: NextFunction): Promise<void> => {
  const token = req.headers.authorization?.split(' ')[1];
  if (!token) { res.status(401).json({ message: 'לא מורשה' }); return; }
  try {
    const decoded = jwt.verify(token, JWT_SECRET) as { id: string };
    const user = await User.findById(decoded.id).select('householdId approved');
    if (!user || !user.approved) { res.status(401).json({ message: 'לא מורשה' }); return; }
    req.userId = decoded.id;
    req.householdId = user.householdId?.toString();
    next();
  } catch {
    res.status(401).json({ message: 'טוקן לא תקין' });
  }
};
