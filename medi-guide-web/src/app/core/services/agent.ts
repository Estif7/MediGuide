import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface AgentDto {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  isAvailable: boolean;
  isActive: boolean;
}

export interface RegisterAgentRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  getAll() {
    return this.http.get<AgentDto[]>(`${this.api}/agents`);
  }

  register(dto: RegisterAgentRequest) {
    return this.http.post(`${this.api}/auth/register-agent`, dto);
  }
}