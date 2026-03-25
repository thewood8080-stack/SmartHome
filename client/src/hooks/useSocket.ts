// hook לניהול חיבור Socket.io
import { useEffect } from 'react';
import { connectSocket, disconnectSocket, getSocket } from '../services/socket';
import { Socket } from 'socket.io-client';

export const useSocket = (): Socket | null => {
  useEffect(() => {
    connectSocket();
    return () => {
      disconnectSocket();
    };
  }, []);

  return getSocket();
};

// hook להאזנה לאירוע ספציפי
export const useSocketEvent = <T>(event: string, handler: (data: T) => void): void => {
  useEffect(() => {
    const socket = connectSocket();
    socket.on(event, handler);
    return () => {
      socket.off(event, handler);
    };
  }, [event, handler]);
};
