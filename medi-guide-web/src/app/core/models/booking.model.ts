export type ResponseTime = 0 | 1 | 2; // Priority | Expedited | Standard
export type BookingStatus = 0 | 1 | 2 | 3 | 4 | 5;

export interface Booking {
  id: string;
  patientId: string;
  patientName: string;
  serviceCategoryId: string;
  categoryName: string;
  agentId: string | null;
  agentName: string | null;
  responseTime: ResponseTime;
  status: BookingStatus;
  amount: number;
  notes: string | null;
  createdAt: string;
}

export interface CreateBookingRequest {
  patientId: string;
  serviceCategoryId: string;
  responseTime: ResponseTime;
  notes?: string;
}