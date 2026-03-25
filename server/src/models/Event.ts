import mongoose, { Schema, Document } from 'mongoose';

export interface IEvent extends Document {
  title: string;
  description?: string;
  date: Date;
  endDate?: Date;
  allDay: boolean;
  color?: string;
  createdBy: mongoose.Types.ObjectId;
  participants: mongoose.Types.ObjectId[];
  reminder?: number; // minutes before
}

const EventSchema = new Schema<IEvent>({
  title:        { type: String, required: true, trim: true },
  description:  { type: String },
  date:         { type: Date, required: true },
  endDate:      { type: Date },
  allDay:       { type: Boolean, default: false },
  color:        { type: String, default: '#5C6BC0' },
  createdBy:    { type: Schema.Types.ObjectId, ref: 'User', required: true },
  participants: [{ type: Schema.Types.ObjectId, ref: 'User' }],
  reminder:     { type: Number },
}, { timestamps: true });

export default mongoose.model<IEvent>('Event', EventSchema);
