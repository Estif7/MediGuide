export interface ChatMessage {
  id: string;
  bookingId: string;
  senderId: string;
  senderRole: string;
  senderName?: string;
  content: string;
  isRead: boolean;
  createdAt: string;
}

export interface CursorPagedResult<T> {
  items: T[];
  hasMore: boolean;
}