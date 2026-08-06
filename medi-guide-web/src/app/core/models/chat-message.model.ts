export interface ChatMessage {
  id: string;
  bookingId: string;
  senderId: string;
  senderRole: string;
  content: string;
  isRead: boolean;
  createdAt: string;
}