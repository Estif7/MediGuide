export interface UserProfile {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  roles: string[];
  patientId?: string;
  agentId?: string;
  createdAt: string;
  title?: string;
  department?: string;
  specialty?: string;
  isAvailable?: boolean;
}

export interface UpdateUserProfile {
  fullName: string;
  phoneNumber?: string;
  currentPassword?: string;
  newPassword?: string;
  title?: string;
  department?: string;
  specialty?: string;
  isAvailable?: boolean;
}

export interface AdminUser {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
  patientId?: string;
  agentId?: string;
  title?: string;
  department?: string;
  specialty?: string;
}

export interface AdminCreateUser {
  fullName: string;
  email: string;
  password?: string;
  phoneNumber?: string;
  role: string;
  title?: string;
  department?: string;
  specialty?: string;
}

export interface AdminUpdateUser {
  fullName: string;
  phoneNumber?: string;
  role?: string;
  isActive?: boolean;
  title?: string;
  department?: string;
  specialty?: string;
}

export interface UserQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  role?: string;
}
