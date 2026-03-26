// store אימות משתמש — Zustand
import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { User } from '../types';

interface Household { name: string; inviteCode: string; }

interface AuthState {
  user: User | null;
  token: string | null;
  household: Household | null;
  setAuth: (user: User, token: string, household?: Household) => void;
  updateUser: (user: User) => void;
  logout: () => void;
  isAdmin: () => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      token: null,
      household: null,
      setAuth: (user, token, household) => set({ user, token, household: household ?? null }),
      updateUser: (user) => set({ user }),
      logout: () => set({ user: null, token: null, household: null }),
      isAdmin: () => get().user?.role === 'admin',
    }),
    { name: 'smarthome-auth' }
  )
);
