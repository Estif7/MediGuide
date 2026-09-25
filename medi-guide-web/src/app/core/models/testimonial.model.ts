export interface Testimonial {
  id: string;
  patientName: string;
  rating: number;
  comment: string;
  createdAt: string;
}

export interface AdminTestimonial {
  id: string;
  patientId: string;
  patientName: string;
  patientEmail: string;
  rating: number;
  comment: string;
  isApproved: boolean;
  bookingId?: string | null;
  createdAt: string;
}

export interface CreateTestimonialRequest {
  rating: number;
  comment: string;
  bookingId?: string | null;
}
