import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Booking, BookingStatus, CreateBookingRequest } from '../models/booking.model';

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

  getById(id: string) {
    return this.http.get<Booking>(`${this.api}/bookings/${id}`);
  }

  confirmPayment(id: string) {
    return this.http.patch<Booking>(`${this.api}/bookings/${id}/confirm-payment`, {});
  }

  assignAgent(bookingId: string, agentId: string) {
    return this.http.patch<Booking>(`${this.api}/bookings/${bookingId}/assign`, { agentId });
  }

  accept(id: string) {
    return this.http.patch<Booking>(`${this.api}/bookings/${id}/accept`, {});
  }

  decline(id: string) {
    return this.http.patch<Booking>(`${this.api}/bookings/${id}/decline`, {});
  }

  refer(id: string, agentId: string) {
    return this.http.patch<Booking>(`${this.api}/bookings/${id}/refer`, { agentId });
  }

  updateBookingStatus(id: string, status: BookingStatus) {
    return this.http.patch<Booking>(`${this.api}/bookings/${id}/booking-status`, { status });
  }
}