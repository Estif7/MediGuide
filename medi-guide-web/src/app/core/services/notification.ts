import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { NotificationDto } from '../models/notification.model';
import { PagedResult } from '../models/booking.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  getAll(page = 1, pageSize = 20, unreadOnly = false) {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (unreadOnly) params = params.set('unreadOnly', true);

    return this.http.get<PagedResult<NotificationDto>>(`${this.api}/notifications`, { params });
  }

  getUnreadCount() {
    return this.http.get<number>(`${this.api}/notifications/unread-count`);
  }

  markRead(id: string) {
    return this.http.patch<void>(`${this.api}/notifications/${id}/read`, {});
  }

  markAllRead() {
    return this.http.patch<void>(`${this.api}/notifications/read-all`, {});
  }
}