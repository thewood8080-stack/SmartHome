// טיפוסי המערכת — SmartHome

export type UserRole = 'admin' | 'member';

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  photoURL?: string;
  points: number;
  createdAt: Date;
  approved: boolean;
}

export interface BaseRecord {
  id: string;
  createdBy: string;
  createdByName: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface Task extends BaseRecord {
  title: string;
  assignedTo?: string;
  assignedToName?: string;
  priority: 'high' | 'medium' | 'low';
  done: boolean;
  doneBy?: string;
  doneByName?: string;
  doneAt?: Date;
  category: string;
  isRecurring: boolean;
  points: number;
}

export interface ShoppingItem extends BaseRecord {
  name: string;
  qty: number;
  unit: string;
  category: string;
  done: boolean;
  doneBy?: string;
  doneByName?: string;
}

export interface Event extends BaseRecord {
  title: string;
  date: Date;
  type: 'birthday' | 'wedding' | 'barmitzvah' | 'medical' | 'other';
  participants: string[];
  giftPersonId?: string;
}

export interface GiftRecord extends BaseRecord {
  personName: string;
  eventType: string;
  eventDate: Date;
  direction: 'gave' | 'received';
  description: string;
}

export interface BudgetRecord extends BaseRecord {
  type: 'income' | 'expense';
  amount: number;
  category: string;
  description: string;
  date: Date;
}

export interface MedicalRecord extends BaseRecord {
  personId: string;
  personName: string;
  doctorName: string;
  date: Date;
  notes: string;
  medications: string[];
  allergies: string[];
}

export interface InventoryItem extends BaseRecord {
  name: string;
  qty: number;
  unit: string;
  minQty: number;
  category: string;
}

export interface VehicleRecord extends BaseRecord {
  licensePlate: string;
  model: string;
  testDate: Date;
  insuranceDate: Date;
  fuelLog: { date: Date; liters: number; cost: number; km: number }[];
}

export interface GamificationRecord {
  userId: string;
  userName: string;
  userPhoto?: string;
  weeklyPoints: number;
  totalPoints: number;
  badges: string[];
  weekStart: Date;
}
