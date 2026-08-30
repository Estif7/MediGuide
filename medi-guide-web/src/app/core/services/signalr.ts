import { Injectable, inject, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { DocumentItem } from '../models/document.model';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth';
import { ChatMessage } from '../models/chat-message.model';
import { Booking } from '../models/booking.model';
import { NotificationDto } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly auth = inject(AuthService);
  private readonly ngZone = inject(NgZone);
  private currentDocumentUploadedHandler?: (document: DocumentItem) => void;
  private hubConnection?: signalR.HubConnection;
  private connectionPromise?: Promise<void>;

  /**
   * Bookings this browser session is currently viewing.
   *
   * SignalR groups are lost when the connection is re-established,
   * so we use this set to automatically rejoin them.
   */
  private readonly joinedBookings = new Set<string>();

  private currentReceiveMessageHandler?: (
    message: ChatMessage
  ) => void;

  private currentBookingUpdatedHandler?: (
    booking: Booking
  ) => void;

  private currentNotificationHandler?: (
    notification: NotificationDto
  ) => void;

  /**
   * Create the SignalR connection if it doesn't already exist.
   */
  private createConnection(): signalR.HubConnection {
    if (this.hubConnection) {
      return this.hubConnection;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(
        `${environment.apiUrl.replace('/api', '')}/hubs/booking`,
        {
          accessTokenFactory: () => this.auth.getToken() ?? '',
        }
      )
      .withAutomaticReconnect()
      .build();

    /**
     * SignalR automatically reconnects the transport, but group
     * membership is not automatically restored by the server.
     *
     * Rejoin every booking that this client was viewing.
     */
    connection.onreconnected(async () => {
      console.log('SignalR reconnected');

      await this.rejoinBookings();
    });

    connection.onreconnecting((error) => {
      console.warn(
        'SignalR reconnecting...',
        error
      );
    });

    connection.onclose((error) => {
      if (error) {
        console.error(
          'SignalR connection closed',
          error
        );
      } else {
        console.log('SignalR connection closed');
      }
    });

    this.hubConnection = connection;

    return connection;
  }

  /**
   * Start the SignalR connection.
   *
   * If a connection attempt is already in progress, all callers
   * share the same promise.
   */
  async connect(): Promise<void> {
    const connection = this.createConnection();

    if (
      connection.state ===
      signalR.HubConnectionState.Connected
    ) {
      return;
    }

    if (
      connection.state ===
      signalR.HubConnectionState.Connecting
    ) {
      if (this.connectionPromise) {
        return this.connectionPromise;
      }

      return;
    }

    if (
      connection.state ===
      signalR.HubConnectionState.Reconnecting
    ) {
      if (this.connectionPromise) {
        return this.connectionPromise;
      }

      return;
    }

    this.connectionPromise = connection
      .start()
      .then(() => {
        console.log('SignalR connected');
      })
      .catch((err) => {
        console.error(
          'SignalR connection failed',
          err
        );

        throw err;
      })
      .finally(() => {
        this.connectionPromise = undefined;
      });

    return this.connectionPromise;
  }

  /**
   * Join a booking SignalR group.
   *
   * The booking is added to joinedBookings only after the server
   * successfully accepts the JoinBooking call.
   */
  async joinBooking(bookingId: string): Promise<void> {
    await this.connect();

    const connection = this.hubConnection;

    if (!connection) {
      throw new Error(
        'SignalR connection is not available.'
      );
    }

    if (
      connection.state !==
      signalR.HubConnectionState.Connected
    ) {
      throw new Error(
        `Cannot join booking. SignalR state is ${connection.state}.`
      );
    }

    try {
      await connection.invoke(
        'JoinBooking',
        bookingId
      );

      this.joinedBookings.add(bookingId);

      console.log(
        `Joined booking ${bookingId}`
      );
    } catch (err) {
      console.error(
        `Failed to join booking ${bookingId}`,
        err
      );

      throw err;
    }
  }

  /**
   * Leave a booking SignalR group.
   *
   * Remove it from joinedBookings even if the connection is
   * currently unavailable. Otherwise a later reconnect would
   * incorrectly rejoin a booking the user already left.
   */
  async leaveBooking(
    bookingId: string
  ): Promise<void> {
    // Remove first so a reconnect cannot rejoin this booking.
    this.joinedBookings.delete(bookingId);

    const connection = this.hubConnection;

    if (!connection) {
      return;
    }

    if (
      connection.state !==
      signalR.HubConnectionState.Connected
    ) {
      return;
    }

    try {
      await connection.invoke(
        'LeaveBooking',
        bookingId
      );

      console.log(
        `Left booking ${bookingId}`
      );
    } catch (err) {
      console.error(
        `Failed to leave booking ${bookingId}`,
        err
      );
    }
  }

  /**
   * Rejoin all bookings that were active before SignalR
   * re-established the connection.
   */
  private async rejoinBookings(): Promise<void> {
    const connection = this.hubConnection;

    if (!connection) {
      return;
    }

    if (
      connection.state !==
      signalR.HubConnectionState.Connected
    ) {
      return;
    }

    const bookings = Array.from(
      this.joinedBookings
    );

    if (bookings.length === 0) {
      return;
    }

    console.log(
      `Rejoining ${bookings.length} booking(s)`
    );

    for (const bookingId of bookings) {
      try {
        await connection.invoke(
          'JoinBooking',
          bookingId
        );

        console.log(
          `Rejoined booking ${bookingId}`
        );
      } catch (err) {
        console.error(
          `Failed to rejoin booking ${bookingId}`,
          err
        );
      }
    }
  }

  /**
   * Register a handler for incoming chat messages.
   */
  onReceiveMessage(
    callback: (message: ChatMessage) => void
  ): void {
    if (this.currentReceiveMessageHandler) {
      this.hubConnection?.off(
        'ReceiveMessage',
        this.currentReceiveMessageHandler
      );
    }

    const zonedHandler = (
      message: ChatMessage
    ) => {
      this.ngZone.run(() => {
        callback(message);
      });
    };

    this.currentReceiveMessageHandler =
      zonedHandler;

    this.hubConnection?.on(
      'ReceiveMessage',
      zonedHandler
    );
  }

  /**
   * Remove the current chat message handler.
   */
  offReceiveMessage(): void {
    if (!this.currentReceiveMessageHandler) {
      return;
    }

    this.hubConnection?.off(
      'ReceiveMessage',
      this.currentReceiveMessageHandler
    );

    this.currentReceiveMessageHandler =
      undefined;
  }

  /**
   * Register a handler for booking updates.
   */
  onBookingUpdated(
    callback: (booking: Booking) => void
  ): void {
    if (this.currentBookingUpdatedHandler) {
      this.hubConnection?.off(
        'BookingUpdated',
        this.currentBookingUpdatedHandler
      );
    }

    const zonedHandler = (
      booking: Booking
    ) => {
      this.ngZone.run(() => {
        callback(booking);
      });
    };

    this.currentBookingUpdatedHandler =
      zonedHandler;

    this.hubConnection?.on(
      'BookingUpdated',
      zonedHandler
    );
  }

  /**
   * Remove the current booking update handler.
   */
  offBookingUpdated(): void {
    if (!this.currentBookingUpdatedHandler) {
      return;
    }

    this.hubConnection?.off(
      'BookingUpdated',
      this.currentBookingUpdatedHandler
    );

    this.currentBookingUpdatedHandler =
      undefined;
  }

  /**
   * Register a handler for real-time notifications.
   */
  onNotificationReceived(
    callback: (
      notification: NotificationDto
    ) => void
  ): void {
    if (this.currentNotificationHandler) {
      this.hubConnection?.off(
        'NotificationReceived',
        this.currentNotificationHandler
      );
    }

    const zonedHandler = (
      notification: NotificationDto
    ) => {
      this.ngZone.run(() => {
        callback(notification);
      });
    };

    this.currentNotificationHandler =
      zonedHandler;

    this.hubConnection?.on(
      'NotificationReceived',
      zonedHandler
    );
  }

  /**
   * Remove the current notification handler.
   */
  offNotificationReceived(): void {
    if (!this.currentNotificationHandler) {
      return;
    }

    this.hubConnection?.off(
      'NotificationReceived',
      this.currentNotificationHandler
    );

    this.currentNotificationHandler =
      undefined;
  }

  onDocumentUploaded(callback: (document: DocumentItem) => void): void {
  if (this.currentDocumentUploadedHandler) {
    this.hubConnection?.off('DocumentUploaded', this.currentDocumentUploadedHandler);
  }

  const zonedHandler = (document: DocumentItem) => {
    this.ngZone.run(() => {
      callback(document);
    });
  };

  this.currentDocumentUploadedHandler = zonedHandler;
  this.hubConnection?.on('DocumentUploaded', zonedHandler);
}

offDocumentUploaded(): void {
  if (!this.currentDocumentUploadedHandler) return;

  this.hubConnection?.off('DocumentUploaded', this.currentDocumentUploadedHandler);
  this.currentDocumentUploadedHandler = undefined;
}
  /**
   * Stop the SignalR connection and clear all local state.
   */
  async disconnect(): Promise<void> {
    const connection = this.hubConnection;

    // Clear joined bookings first so reconnect logic cannot
    // accidentally restore stale booking memberships.
    this.joinedBookings.clear();

    if (!connection) {
      return;
    }

    try {
      if (
        connection.state !==
        signalR.HubConnectionState.Disconnected
      ) {
        await connection.stop();
      }
    } catch (err) {
      console.error(
        'SignalR disconnect failed',
        err
      );
    } finally {
      this.hubConnection = undefined;
      this.connectionPromise = undefined;

      this.currentReceiveMessageHandler =
        undefined;

      this.currentBookingUpdatedHandler =
        undefined;

      this.currentNotificationHandler =
        undefined;
      
      this.currentDocumentUploadedHandler = 
        undefined;
    }
  }
}
