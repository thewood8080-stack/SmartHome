import mongoose, { Schema, Document } from 'mongoose';

export interface IShoppingItem extends Document {
  name: string;
  quantity: number;
  unit?: string;
  category?: string;
  addedBy: mongoose.Types.ObjectId;
  bought: boolean;
  boughtBy?: mongoose.Types.ObjectId;
  boughtAt?: Date;
  urgent: boolean;
}

const ShoppingItemSchema = new Schema<IShoppingItem>({
  name:     { type: String, required: true, trim: true },
  quantity: { type: Number, default: 1 },
  unit:     { type: String },
  category: { type: String },
  addedBy:  { type: Schema.Types.ObjectId, ref: 'User', required: true },
  bought:   { type: Boolean, default: false },
  boughtBy: { type: Schema.Types.ObjectId, ref: 'User' },
  boughtAt: { type: Date },
  urgent:   { type: Boolean, default: false },
}, { timestamps: true });

export default mongoose.model<IShoppingItem>('ShoppingItem', ShoppingItemSchema);
