export interface InternalNote {
  id: string;
  bookingId: string;
  agentId: string;
  agentName: string;
  content: string;
  createdAt: string;
}

export interface CreateInternalNoteRequest {
  content: string;
}
