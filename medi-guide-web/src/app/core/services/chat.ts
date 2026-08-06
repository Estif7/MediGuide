import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ChatMessage } from '../models/chat-message.model';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  getByBooking(bookingId: string) {
    return this.http.get<ChatMessage[]>(`${this.api}/chatmessages/booking/${bookingId}`);
  }

  send(bookingId: string, content: string) {
    return this.http.post<ChatMessage>(`${this.api}/chatmessages/booking/${bookingId}`, {
      content,
    });
  }
}