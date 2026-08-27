import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/booking.model';

export interface PatientDto {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  preferredLanguage: string | null;
  isActive: boolean;
}

export interface PatientQuery {
  page?: number;
  pageSize?: number;
  name?: string;
}

@Injectable({ providedIn: 'root' })
export class PatientService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  getAll(query: PatientQuery = {}) {
    let params = new HttpParams();
    if (query.page != null) params = params.set('page', query.page);
    if (query.pageSize != null) params = params.set('pageSize', query.pageSize);
    if (query.name) params = params.set('name', query.name);

    return this.http.get<PagedResult<PatientDto>>(`${this.api}/patients`, { params });
  }
}