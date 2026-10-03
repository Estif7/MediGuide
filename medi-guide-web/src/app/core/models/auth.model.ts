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
  dateOfBirth?: string;
  gender?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  allergies?: string;
  chronicConditions?: string;
  currentMedications?: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  email: string;
  fullName: string;
  roles: string[];
  patientId: string | null;
  agentId: string | null;
}
