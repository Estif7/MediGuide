import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { DocumentItem } from '../models/document.model';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  getByBooking(bookingId: string) {
    return this.http.get<DocumentItem[]>(`${this.api}/documents/booking/${bookingId}`);
  }

  upload(bookingId: string, file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<DocumentItem>(
      `${this.api}/documents/booking/${bookingId}`,
      form
    );
  }
}