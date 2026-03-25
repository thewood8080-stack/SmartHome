import mongoose, { Schema, Document } from 'mongoose';

export interface IGift extends Document {
  recipientName: string;
  occasion: string;
  date: Date;
  ideas: string[];
  budget?: number;
  purchased: boolean;
  purchasedItem?: string;
  purchasedPrice?: number;
  note?: string;
  addedBy: mongoose.Types.ObjectId;
}

const GiftSchema = new Schema<IGift>({
  recipientName:  { type: String, required: true, trim: true },
  occasion:       { type: String, required: true },
  date:           { type: Date, required: true },
  ideas:          [{ type: String }],
  budget:         { type: Number },
  purchased:      { type: Boolean, default: false },
  purchasedItem:  { type: String },
  purchasedPrice: { type: Number },
  note:           { type: String },
  addedBy:        { type: Schema.Types.ObjectId, ref: 'User', required: true },
}, { timestamps: true });

export default mongoose.model<IGift>('Gift', GiftSchema);
