import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ChatMessage, CursorPagedResult } from '../models/chat-message.model';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  getByBooking(bookingId: string, before?: string, pageSize = 30) {
    let params = new HttpParams().set('pageSize', pageSize);
    if (before) params = params.set('before', before);

    return this.http.get<CursorPagedResult<ChatMessage>>(
      `${this.api}/chatmessages/booking/${bookingId}`,
      { params }
    );
  }

  send(bookingId: string, content: string) {
    return this.http.post<ChatMessage>(`${this.api}/chatmessages/booking/${bookingId}`, {
      content,
    });
  }
}