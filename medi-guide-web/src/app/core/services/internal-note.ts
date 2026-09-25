import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { InternalNote, CreateInternalNoteRequest } from '../models/internal-note.model';

@Injectable({
  providedIn: 'root',
})
export class InternalNoteService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/bookings`;

  getByBooking(bookingId: string): Observable<InternalNote[]> {
    return this.http.get<InternalNote[]>(`${this.baseUrl}/${bookingId}/internal-notes`);
  }

  create(bookingId: string, content: string): Observable<InternalNote> {
    const payload: CreateInternalNoteRequest = { content };
    return this.http.post<InternalNote>(`${this.baseUrl}/${bookingId}/internal-notes`, payload);
  }
}
