export interface NotificationDto {
  id: string;
  type: string;
  message: string;
  bookingId: string | null;
  isRead: boolean;
  createdAt: string;
}