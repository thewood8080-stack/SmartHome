// מנהל Socket.io — עדכונים בזמן אמת
import { Server as HttpServer } from 'http';
import { Server as SocketServer, Socket } from 'socket.io';

let io: SocketServer;

export const initSocket = (httpServer: HttpServer): SocketServer => {
  io = new SocketServer(httpServer, {
    cors: {
      origin: process.env.CLIENT_URL || 'http://localhost:5173',
      methods: ['GET', 'POST'],
      credentials: true,
    },
  });

  io.on('connection', (socket: Socket) => {
    console.log(`🔌 משתמש התחבר: ${socket.id}`);

    socket.on('disconnect', () => {
      console.log(`🔌 משתמש התנתק: ${socket.id}`);
    });
  });

  return io;
};

// שליחת אירוע לכל המחוברים
export const emitToAll = (event: string, data: unknown): void => {
  if (io) io.emit(event, data);
};

export const getIO = (): SocketServer => io;
