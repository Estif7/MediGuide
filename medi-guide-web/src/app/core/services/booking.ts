import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Booking, CreateBookingRequest } from '../models/booking.model';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  getAll() {
    return this.http.get<Booking[]>(`${this.api}/bookings`);
  }

  create(dto: CreateBookingRequest) {
    return this.http.post<Booking>(`${this.api}/bookings`, dto);
  }
}