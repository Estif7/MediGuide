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
  isReferralPendingApproval?: boolean;
  referredToAgentId?: string | null;
  referredToAgentName?: string | null;
  referralReason?: string | null;
  referralClinicalNotes?: string | null;
}

export interface CreateBookingRequest {
  patientId: string;
  serviceCategoryId: string;
  responseTime: ResponseTime;
  notes?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface SimulatePaymentRequest {
  paymentMethod?: string;
  transactionReference?: string;
}

export interface ReferBookingRequest {
  targetAgentId: string;
  reason: string;
  clinicalNotes?: string;
}

export interface RejectReferralRequest {
  reason?: string;
}

export interface BookingQuery {
  page?: number;
  pageSize?: number;
  status?: BookingStatus;
  patientName?: string;
  agentName?: string;
  sortBy?: 'CreatedAt' | 'Status';
  sortDir?: 'asc' | 'desc';
}