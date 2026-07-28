// hook לניהול חיבור Socket.io
import { useEffect } from 'react';
import { connectSocket, disconnectSocket } from '../services/socket';
import { useAuthStore } from '../store/authStore';
import { Socket } from 'socket.io-client';

export const useSocket = (): Socket | null => {
  const { household } = useAuthStore();

  useEffect(() => {
    // חיבור + הצטרפות לחדר הבית
    const socket = connectSocket(household?.id);
    return () => {
      disconnectSocket();
    };
  }, [household?.id]);

  return null;
};

// hook להאזנה לאירוע ספציפי
export const useSocketEvent = <T>(event: string, handler: (data: T) => void): void => {
  const { household } = useAuthStore();

  useEffect(() => {
    const socket = connectSocket(household?.id);
    socket.on(event, handler);
    return () => {
      socket.off(event, handler);
    };
  }, [event, handler, household?.id]);
};
