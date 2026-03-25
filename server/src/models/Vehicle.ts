import mongoose, { Schema, Document } from 'mongoose';

export interface IVehicle extends Document {
  name: string;
  plateNumber?: string;
  year?: number;
  lastService?: Date;
  nextService?: Date;
  insurance?: Date;
  test?: Date; // טסט
  fuelType?: string;
  notes?: string;
  addedBy: mongoose.Types.ObjectId;
}

const VehicleSchema = new Schema<IVehicle>({
  name:        { type: String, required: true, trim: true },
  plateNumber: { type: String },
  year:        { type: Number },
  lastService: { type: Date },
  nextService: { type: Date },
  insurance:   { type: Date },
  test:        { type: Date },
  fuelType:    { type: String },
  notes:       { type: String },
  addedBy:     { type: Schema.Types.ObjectId, ref: 'User', required: true },
}, { timestamps: true });

export default mongoose.model<IVehicle>('Vehicle', VehicleSchema);
