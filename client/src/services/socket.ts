// חיבור Socket.io לצד הלקוח
import { io, Socket } from 'socket.io-client';

const SOCKET_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

let socket: Socket | null = null;

export const connectSocket = (householdId?: string | null): Socket => {
  if (!socket) {
    socket = io(SOCKET_URL, { withCredentials: true });
  }
  if (householdId) {
    // הצטרפות לחדר הבית — עדכונים בזמן אמת לבני הבית בלבד
    socket.emit('join:household', householdId);
  }
  return socket;
};

export const disconnectSocket = (): void => {
  if (socket) {
    socket.disconnect();
    socket = null;
  }
};

export const getSocket = (): Socket | null => socket;
