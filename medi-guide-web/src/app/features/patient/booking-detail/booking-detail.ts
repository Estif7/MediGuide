import { Component, inject, signal, OnInit, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { BookingService } from '../../../core/services/booking';
import { ChatService } from '../../../core/services/chat';
import { DocumentService } from '../../../core/services/document';
import { ChatMessage } from '../../../core/models/chat-message.model';
import { DocumentItem } from '../../../core/models/document.model';
import { DatePipe } from '@angular/common';
import { AgentService, AgentDto } from '../../../core/services/agent';
import { AuthService } from '../../../core/services/auth';
import { bookingStatusLabel, responseTimeLabel } from '../../../core/utils/status-label';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { Booking, BookingStatus } from '../../../core/models/booking.model';

@Component({
  selector: 'app-booking-detail',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe, MatFormFieldModule, MatSelectModule],
  templateUrl: './booking-detail.html',
  styleUrl: './booking-detail.scss',
})
export class BookingDetail implements OnInit {
  // route param: /patient/bookings/:id
  id = input.required<string>();

  private readonly bookingService = inject(BookingService);
  private readonly chatService = inject(ChatService);
  private readonly documentService = inject(DocumentService);

  private readonly agentService = inject(AgentService);
  private readonly auth = inject(AuthService);

  agents = signal<AgentDto[]>([]);
  selectedAgentId = signal('');
  isAdmin = this.auth.isAdmin;
  isAgent = this.auth.isAgent;

  booking = signal<Booking | null>(null);
  messages = signal<ChatMessage[]>([]);
  documents = signal<DocumentItem[]>([]);
  newMessage = signal('');
  message = signal<string | null>(null);
  loading = signal(false);
  statusLabel = bookingStatusLabel;
  timeLabel = responseTimeLabel;

  ngOnInit() {
    this.loadAll();

    this.agentService.getAll().subscribe({
      next: (a) => this.agents.set(a),
    });
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

  confirmPayment() {
    this.bookingService.confirmPayment(this.id()).subscribe({
      next: (b) => { this.booking.set(b); this.message.set('Payment confirmed'); },
      error: (e) => this.message.set(e.error || 'Failed'),
    });
  }

  assignAgent() {
    const agentId = this.selectedAgentId();
    if (!agentId) {
      this.message.set('Select an agent');
      return;
    }

    this.loading.set(true);
    this.bookingService.assignAgent(this.id(), agentId).subscribe({
      next: (updated) => {
        this.booking.set(updated);
        this.message.set('Agent assigned');
        this.loading.set(false);
      },
      error: (err) => {
        this.message.set(err.error || 'Assign failed');
        this.loading.set(false);
      },
    });
  }

  accept() {
    this.bookingService.accept(this.id()).subscribe({
      next: (b) => { this.booking.set(b); this.message.set('Booking accepted'); },
      error: (e) => this.message.set(e.error || 'Failed'),
    });
  }

  decline() {
    this.bookingService.decline(this.id()).subscribe({
      next: (b) => { this.booking.set(b); this.message.set('Booking declined'); },
      error: (e) => this.message.set(e.error || 'Failed'),
    });
  }

  refer() {
    const agentId = this.selectedAgentId();
    if (!agentId) { this.message.set('Select an agent'); return; }
    this.bookingService.refer(this.id(), agentId).subscribe({
      next: (b) => { this.booking.set(b); this.message.set('Referred to another agent'); },
      error: (e) => this.message.set(e.error || 'Failed'),
    });
  }

  docDownloadUrl(id: string) {
    return this.documentService.downloadUrl(id);
  }

  allowedStatuses: BookingStatus[] = [0, 1, 2, 3]; // PendingPayment, Paid, Assigned, InProgress

  updateStatus(newStatus: BookingStatus) {
    this.bookingService.updateBookingStatus(this.id(), newStatus).subscribe({
      next: (updated) => {
        this.booking.set(updated);
        this.message.set(`Status updated to ${this.statusLabel(newStatus)}`);
      },
      error: (err) => this.message.set(err.error?.title || 'Failed to update status'),
    });
  }

}