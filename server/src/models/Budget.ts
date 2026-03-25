import mongoose, { Schema, Document } from 'mongoose';

export interface IBudget extends Document {
  title: string;
  amount: number;
  type: 'income' | 'expense';
  category: string;
  date: Date;
  note?: string;
  addedBy: mongoose.Types.ObjectId;
}

const BudgetSchema = new Schema<IBudget>({
  title:    { type: String, required: true, trim: true },
  amount:   { type: Number, required: true },
  type:     { type: String, enum: ['income', 'expense'], required: true },
  category: { type: String, required: true },
  date:     { type: Date, required: true },
  note:     { type: String },
  addedBy:  { type: Schema.Types.ObjectId, ref: 'User', required: true },
}, { timestamps: true });

export default mongoose.model<IBudget>('Budget', BudgetSchema);
