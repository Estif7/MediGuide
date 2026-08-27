import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth';
import { ChatMessage } from '../models/chat-message.model';
import { Booking } from '../models/booking.model';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly auth = inject(AuthService);
  private hubConnection?: signalR.HubConnection;

  private currentReceiveMessageHandler?: (message: ChatMessage) => void;
  private currentBookingUpdatedHandler?: (booking: Booking) => void;

  connect() {
    if (this.hubConnection) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace('/api', '')}/hubs/booking`, {
        accessTokenFactory: () => this.auth.getToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start().catch((err) => console.error('SignalR connection failed', err));
  }

  joinBooking(bookingId: string) {
    this.hubConnection?.invoke('JoinBooking', bookingId).catch((err) => console.error(err));
  }

  leaveBooking(bookingId: string) {
    this.hubConnection?.invoke('LeaveBooking', bookingId).catch((err) => console.error(err));
  }

  onReceiveMessage(callback: (message: ChatMessage) => void) {
    if (this.currentReceiveMessageHandler) {
      this.hubConnection?.off('ReceiveMessage', this.currentReceiveMessageHandler);
    }
    this.currentReceiveMessageHandler = callback;
    this.hubConnection?.on('ReceiveMessage', callback);
  }

  offReceiveMessage() {
    if (this.currentReceiveMessageHandler) {
      this.hubConnection?.off('ReceiveMessage', this.currentReceiveMessageHandler);
      this.currentReceiveMessageHandler = undefined;
    }
  }

  onBookingUpdated(callback: (booking: Booking) => void) {
    if (this.currentBookingUpdatedHandler) {
      this.hubConnection?.off('BookingUpdated', this.currentBookingUpdatedHandler);
    }
    this.currentBookingUpdatedHandler = callback;
    this.hubConnection?.on('BookingUpdated', callback);
  }

  offBookingUpdated() {
    if (this.currentBookingUpdatedHandler) {
      this.hubConnection?.off('BookingUpdated', this.currentBookingUpdatedHandler);
      this.currentBookingUpdatedHandler = undefined;
    }
  }

  disconnect() {
    this.hubConnection?.stop();
    this.hubConnection = undefined;
    this.currentReceiveMessageHandler = undefined;
    this.currentBookingUpdatedHandler = undefined;
  }
}