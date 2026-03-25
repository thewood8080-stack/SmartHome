import mongoose, { Schema, Document } from 'mongoose';

export interface IMedical extends Document {
  memberId: mongoose.Types.ObjectId;
  type: 'appointment' | 'medication' | 'vaccine' | 'note';
  title: string;
  date: Date;
  doctor?: string;
  clinic?: string;
  notes?: string;
  nextAppointment?: Date;
  addedBy: mongoose.Types.ObjectId;
}

const MedicalSchema = new Schema<IMedical>({
  memberId:        { type: Schema.Types.ObjectId, ref: 'User', required: true },
  type:            { type: String, enum: ['appointment', 'medication', 'vaccine', 'note'], required: true },
  title:           { type: String, required: true, trim: true },
  date:            { type: Date, required: true },
  doctor:          { type: String },
  clinic:          { type: String },
  notes:           { type: String },
  nextAppointment: { type: Date },
  addedBy:         { type: Schema.Types.ObjectId, ref: 'User', required: true },
}, { timestamps: true });

export default mongoose.model<IMedical>('Medical', MedicalSchema);
