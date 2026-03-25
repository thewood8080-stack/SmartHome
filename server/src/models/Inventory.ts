import mongoose, { Schema, Document } from 'mongoose';

export interface IInventory extends Document {
  name: string;
  category: string;
  quantity: number;
  unit?: string;
  minQuantity?: number;
  location?: string;
  expiryDate?: Date;
  note?: string;
  addedBy: mongoose.Types.ObjectId;
}

const InventorySchema = new Schema<IInventory>({
  name:        { type: String, required: true, trim: true },
  category:    { type: String, required: true },
  quantity:    { type: Number, required: true, default: 0 },
  unit:        { type: String },
  minQuantity: { type: Number, default: 0 },
  location:    { type: String },
  expiryDate:  { type: Date },
  note:        { type: String },
  addedBy:     { type: Schema.Types.ObjectId, ref: 'User', required: true },
}, { timestamps: true });

export default mongoose.model<IInventory>('Inventory', InventorySchema);
