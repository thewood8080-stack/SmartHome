// hook לניהול חיבור SignalR
import { useEffect } from 'react';
import { HubConnection } from '@microsoft/signalr';
import { connectSocket, disconnectSocket } from '../services/socket';
import { useAuthStore } from '../store/authStore';

export const useSocket = (): HubConnection | null => {
  const { household } = useAuthStore();

  useEffect(() => {
    // השיוך לקבוצת הבית נעשה בשרת לפי המשתמש המחובר
    connectSocket(household?.id);
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
    const connection = connectSocket(household?.id);
    connection.on(event, handler);
    return () => {
      connection.off(event, handler);
    };
  }, [event, handler, household?.id]);
};
