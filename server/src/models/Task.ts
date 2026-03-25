import mongoose, { Schema, Document } from 'mongoose';

export interface ITask extends Document {
  title: string;
  description?: string;
  assignedTo?: mongoose.Types.ObjectId;
  createdBy: mongoose.Types.ObjectId;
  dueDate?: Date;
  priority: 'low' | 'medium' | 'high';
  status: 'todo' | 'inprogress' | 'done';
  points: number;
  recurring?: 'none' | 'daily' | 'weekly' | 'monthly';
  completedAt?: Date;
}

const TaskSchema = new Schema<ITask>({
  title:       { type: String, required: true, trim: true },
  description: { type: String },
  assignedTo:  { type: Schema.Types.ObjectId, ref: 'User' },
  createdBy:   { type: Schema.Types.ObjectId, ref: 'User', required: true },
  dueDate:     { type: Date },
  priority:    { type: String, enum: ['low', 'medium', 'high'], default: 'medium' },
  status:      { type: String, enum: ['todo', 'inprogress', 'done'], default: 'todo' },
  points:      { type: Number, default: 10 },
  recurring:   { type: String, enum: ['none', 'daily', 'weekly', 'monthly'], default: 'none' },
  completedAt: { type: Date },
}, { timestamps: true });

export default mongoose.model<ITask>('Task', TaskSchema);
