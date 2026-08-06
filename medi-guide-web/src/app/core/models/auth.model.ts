export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterPatientRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
  preferredLanguage?: string;
}

export interface AuthResponse {
  token: string;
  email: string;
  fullName: string;
  roles: string[];
  patientId: string | null;
  agentId: string | null;
}