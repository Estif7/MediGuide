import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Booking, BookingQuery, BookingStatus, CreateBookingRequest, PagedResult, ReferBookingRequest, SimulatePaymentRequest } from '../models/booking.model';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  getAll(query: BookingQuery = {}) {
    let params = new HttpParams();
    if (query.page != null) params = params.set('page', query.page);
    if (query.pageSize != null) params = params.set('pageSize', query.pageSize);
    if (query.status != null) params = params.set('status', query.status);
    if (query.patientName) params = params.set('patientName', query.patientName);
    if (query.agentName) params = params.set('agentName', query.agentName);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDir) params = params.set('sortDir', query.sortDir);

    return this.http.get<PagedResult<Booking>>(`${this.api}/bookings`, { params });
  }

  create(dto: CreateBookingRequest) {
    return this.http.post<Booking>(`${this.api}/bookings`, dto);
  }

  getById(id: string) {
    return this.http.get<Booking>(`${this.api}/bookings/${id}`);
  }

  simulatePayment(id: string, dto: SimulatePaymentRequest = {}) {
    return this.http.post<Booking>(`${this.api}/bookings/${id}/simulate-payment`, dto);
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

  complete(id: string) {
    return this.http.patch<Booking>(`${this.api}/bookings/${id}/complete`, {});
  }

  refer(id: string, dto: ReferBookingRequest) {
    return this.http.patch<Booking>(`${this.api}/bookings/${id}/refer`, dto);
  }

  approveReferral(id: string) {
    return this.http.patch<Booking>(`${this.api}/bookings/${id}/approve-referral`, {});
  }

  rejectReferral(id: string, reason?: string) {
    return this.http.patch<Booking>(`${this.api}/bookings/${id}/reject-referral`, { reason });
  }


  updateBookingStatus(id: string, status: BookingStatus) {
    return this.http.patch<Booking>(`${this.api}/bookings/${id}/booking-status`, { status });
  }
}