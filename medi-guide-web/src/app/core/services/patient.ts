import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface PatientDto {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  preferredLanguage: string | null;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class PatientService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  getAll() {
    return this.http.get<PatientDto[]>(`${this.api}/patients`);
  }
}