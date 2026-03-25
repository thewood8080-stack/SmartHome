import mongoose, { Schema, Document } from 'mongoose';

export interface IShortcut extends Document {
  label: string;
  url: string;
  icon?: string;
  color?: string;
  order: number;
  addedBy: mongoose.Types.ObjectId;
}

const ShortcutSchema = new Schema<IShortcut>({
  label:   { type: String, required: true, trim: true },
  url:     { type: String, required: true },
  icon:    { type: String },
  color:   { type: String, default: '#5C6BC0' },
  order:   { type: Number, default: 0 },
  addedBy: { type: Schema.Types.ObjectId, ref: 'User', required: true },
}, { timestamps: true });

export default mongoose.model<IShortcut>('Shortcut', ShortcutSchema);
