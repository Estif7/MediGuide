import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/booking.model';

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

export interface AgentQuery {
  page?: number;
  pageSize?: number;
  name?: string;
}

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  getAll(query: AgentQuery = {}) {
    let params = new HttpParams();
    if (query.page != null) params = params.set('page', query.page);
    if (query.pageSize != null) params = params.set('pageSize', query.pageSize);
    if (query.name) params = params.set('name', query.name);

    return this.http.get<PagedResult<AgentDto>>(`${this.api}/agents`, { params });
  }

  register(dto: RegisterAgentRequest) {
    return this.http.post(`${this.api}/auth/register-agent`, dto);
  }
}