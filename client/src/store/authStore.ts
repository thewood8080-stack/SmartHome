// store אימות משתמש — Zustand
import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { User } from '../types';

interface AuthState {
  user: User | null;
  token: string | null;
  setAuth: (user: User, token: string) => void;
  updateUser: (user: User) => void;
  logout: () => void;
  isAdmin: () => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      token: null,
      setAuth: (user, token) => set({ user, token }),
      updateUser: (user) => set({ user }),
      logout: () => set({ user: null, token: null }),
      isAdmin: () => get().user?.role === 'admin',
    }),
    { name: 'smarthome-auth' }
  )
);
