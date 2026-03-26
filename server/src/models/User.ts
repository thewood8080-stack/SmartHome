// מודל משתמש — SmartHome
import mongoose, { Schema, Document } from 'mongoose';

export interface IUser extends Document {
  name: string;
  email: string;
  password: string;
  role: 'admin' | 'member';
  photoURL?: string;
  points: number;
  approved: boolean;
  householdId: mongoose.Types.ObjectId;
  createdAt: Date;
}

const UserSchema = new Schema<IUser>({
  name:        { type: String, required: true, trim: true },
  email:       { type: String, required: true, unique: true, lowercase: true },
  password:    { type: String, required: true },
  role:        { type: String, enum: ['admin', 'member'], default: 'member' },
  photoURL:    { type: String },
  points:      { type: Number, default: 0 },
  approved:    { type: Boolean, default: false },
  householdId: { type: Schema.Types.ObjectId, ref: 'Household' },
}, { timestamps: true });

export default mongoose.model<IUser>('User', UserSchema);
