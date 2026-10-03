import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Testimonial,
  AdminTestimonial,
  CreateTestimonialRequest,
} from '../models/testimonial.model';

@Injectable({
  providedIn: 'root',
})
export class TestimonialService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/testimonials`;

  getApproved(): Observable<Testimonial[]> {
    return this.http.get<Testimonial[]>(this.baseUrl);
  }

  getForAdmin(): Observable<AdminTestimonial[]> {
    return this.http.get<AdminTestimonial[]>(`${this.baseUrl}/admin`);
  }

  submit(req: CreateTestimonialRequest): Observable<Testimonial> {
    return this.http.post<Testimonial>(this.baseUrl, req);
  }

  approve(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/approve`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
