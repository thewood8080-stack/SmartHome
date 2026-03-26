// מודל תא משפחתי
import mongoose, { Schema, Document } from 'mongoose';

export interface IHousehold extends Document {
  name: string;
  inviteCode: string;
  createdBy: mongoose.Types.ObjectId;
}

const HouseholdSchema = new Schema<IHousehold>({
  name:       { type: String, required: true, trim: true },
  inviteCode: { type: String, required: true, unique: true, uppercase: true },
  createdBy:  { type: Schema.Types.ObjectId, ref: 'User', required: true },
}, { timestamps: true });

export default mongoose.model<IHousehold>('Household', HouseholdSchema);
