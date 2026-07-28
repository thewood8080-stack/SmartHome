// מנהל Socket.io — עדכונים בזמן אמת לפי תא משפחתי
import { Server as HttpServer } from 'http';
import { Server as SocketServer, Socket } from 'socket.io';

let io: SocketServer;

const allowedOrigins = [
  'http://localhost:5173',
  'http://localhost:3000',
  process.env.CLIENT_URL,
].filter(Boolean) as string[];

export const initSocket = (httpServer: HttpServer): SocketServer => {
  io = new SocketServer(httpServer, {
    cors: {
      origin: allowedOrigins,
      methods: ['GET', 'POST'],
      credentials: true,
    },
  });

  io.on('connection', (socket: Socket) => {
    // כל משתמש מצטרף לחדר של הבית שלו
    socket.on('join:household', (householdId: string) => {
      socket.join(householdId);
      console.log(`🏠 socket ${socket.id} הצטרף לבית ${householdId}`);
    });

    socket.on('disconnect', () => {
      console.log(`🔌 משתמש התנתק: ${socket.id}`);
    });
  });

  return io;
};

// שליחת אירוע לכל בני הבית בלבד
export const emitToHousehold = (householdId: string | undefined, event: string, data: unknown): void => {
  if (io && householdId) io.to(householdId).emit(event, data);
};

export const getIO = (): SocketServer => io;
