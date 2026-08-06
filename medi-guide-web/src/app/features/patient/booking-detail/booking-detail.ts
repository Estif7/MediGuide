import { Component, inject, signal, OnInit, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { BookingService } from '../../../core/services/booking';
import { ChatService } from '../../../core/services/chat';
import { DocumentService } from '../../../core/services/document';
import { Booking } from '../../../core/models/booking.model';
import { ChatMessage } from '../../../core/models/chat-message.model';
import { DocumentItem } from '../../../core/models/document.model';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-booking-detail',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe],
  templateUrl: './booking-detail.html',
  styleUrl: './booking-detail.scss',
})
export class BookingDetail implements OnInit {
  // route param: /patient/bookings/:id
  id = input.required<string>();

  private readonly bookingService = inject(BookingService);
  private readonly chatService = inject(ChatService);
  private readonly documentService = inject(DocumentService);

  booking = signal<Booking | null>(null);
  messages = signal<ChatMessage[]>([]);
  documents = signal<DocumentItem[]>([]);
  newMessage = signal('');
  message = signal<string | null>(null);
  loading = signal(false);

  ngOnInit() {
    this.loadAll();
  }

  loadAll() {
    const bookingId = this.id();

    this.bookingService.getById(bookingId).subscribe({
      next: (b) => this.booking.set(b),
      error: () => this.message.set('Booking not found'),
    });

    this.chatService.getByBooking(bookingId).subscribe({
      next: (m) => this.messages.set(m),
    });

    this.documentService.getByBooking(bookingId).subscribe({
      next: (d) => this.documents.set(d),
    });
  }

  sendMessage() {
    const content = this.newMessage().trim();
    if (!content) return;

    this.loading.set(true);
    this.chatService.send(this.id(), content).subscribe({
      next: (msg) => {
        this.messages.update((list) => [...list, msg]);
        this.newMessage.set('');
        this.loading.set(false);
      },
      error: () => {
        this.message.set('Failed to send message');
        this.loading.set(false);
      },
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.loading.set(true);
    this.documentService.upload(this.id(), file).subscribe({
      next: (doc) => {
        this.documents.update((list) => [doc, ...list]);
        this.loading.set(false);
        input.value = '';
      },
      error: () => {
        this.message.set('Upload failed');
        this.loading.set(false);
      },
    });
  }
}