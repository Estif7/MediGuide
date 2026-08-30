import {
  Component,
  inject,
  signal,
  OnInit,
  OnDestroy,
  input,
  ViewChild,
  ElementRef,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { BookingService } from '../../../core/services/booking';
import { ChatService } from '../../../core/services/chat';
import { DocumentService } from '../../../core/services/document';
import { SignalRService } from '../../../core/services/signalr';
import { AuthService } from '../../../core/services/auth';
import { ReferenceDataService } from '../../../core/services/reference-data';
import { StatePanel } from '../../../shared/state-panel/state-panel';

import { Booking, BookingStatus } from '../../../core/models/booking.model';
import { ChatMessage } from '../../../core/models/chat-message.model';
import { DocumentItem } from '../../../core/models/document.model';

import {
  bookingStatusLabel,
  responseTimeLabel,
} from '../../../core/utils/status-label';

@Component({
  selector: 'app-booking-detail',
  standalone: true,
  imports: [
    FormsModule,
    DatePipe,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    StatePanel,
  ],
  templateUrl: './booking-detail.html',
  styleUrl: './booking-detail.scss',
})
export class BookingDetail implements OnInit, OnDestroy {
  id = input.required<string>();

  private readonly bookingService = inject(BookingService);
  private readonly chatService = inject(ChatService);
  private readonly documentService = inject(DocumentService);
  private readonly referenceData = inject(ReferenceDataService);
  private readonly auth = inject(AuthService);
  private readonly signalR = inject(SignalRService);

  @ViewChild('chatScroll')
  chatScrollRef?: ElementRef<HTMLDivElement>;

  agents = this.referenceData.agents;
  selectedAgentId = signal('');

  isAdmin = this.auth.isAdmin;
  isAgent = this.auth.isAgent;

  booking = signal<Booking | null>(null);
  bookingLoading = signal(false);
  bookingError = signal<string | null>(null);

  messages = signal<ChatMessage[]>([]);
  messagesLoading = signal(false);
  messagesError = signal<string | null>(null);
  hasMoreMessages = signal(false);
  loadingMore = signal(false);

  documents = signal<DocumentItem[]>([]);
  documentsLoading = signal(false);
  documentsError = signal<string | null>(null);

  newMessage = signal('');
  message = signal<string | null>(null); // success/info toasts only now
  loading = signal(false);

  statusLabel = bookingStatusLabel;
  timeLabel = responseTimeLabel;

  allowedStatuses: BookingStatus[] = [0, 1, 2, 3];

  ngOnInit() {
    this.loadBooking();
    this.loadMessages();
    this.loadDocuments();
    this.referenceData.loadAgents();

    this.signalR.onReceiveMessage((msg) => {
      if (msg.bookingId !== this.id()) return;
      if (this.messages().some((m) => m.id === msg.id)) return;

      this.messages.update((list) => [...list, msg]);
      setTimeout(() => this.scrollToBottom());
    });

    this.signalR.onBookingUpdated((updated) => {
      if (updated.id !== this.id()) return;
      this.booking.set(updated);
    });

    this.signalR.onDocumentUploaded((doc) => {
      if (doc.bookingId !== this.id()) return;
      if (this.documents().some((d) => d.id === doc.id)) return;

      this.documents.update((list) => [doc, ...list]);
    });

    this.signalR.joinBooking(this.id()).catch((err) => {
      console.error('Failed to join booking SignalR group', err);
    });
  }

  ngOnDestroy() {
    this.signalR.leaveBooking(this.id());
    this.signalR.offReceiveMessage();
    this.signalR.offBookingUpdated();
    this.signalR.offDocumentUploaded();
  }

  loadBooking() {
    this.bookingLoading.set(true);
    this.bookingError.set(null);

    this.bookingService.getById(this.id()).subscribe({
      next: (b) => {
        this.booking.set(b);
        this.bookingLoading.set(false);
      },
      error: () => {
        this.bookingError.set('Booking not found');
        this.bookingLoading.set(false);
      },
    });
  }

  loadMessages() {
    this.messagesLoading.set(true);
    this.messagesError.set(null);

    this.chatService.getByBooking(this.id()).subscribe({
      next: (result) => {
        this.messages.set(result.items);
        this.hasMoreMessages.set(result.hasMore);
        this.messagesLoading.set(false);
        setTimeout(() => this.scrollToBottom());
      },
      error: () => {
        this.messagesError.set('Failed to load messages');
        this.messagesLoading.set(false);
      },
    });
  }

  loadDocuments() {
    this.documentsLoading.set(true);
    this.documentsError.set(null);

    this.documentService.getByBooking(this.id()).subscribe({
      next: (d) => {
        this.documents.set(d);
        this.documentsLoading.set(false);
      },
      error: () => {
        this.documentsError.set('Failed to load documents');
        this.documentsLoading.set(false);
      },
    });
  }

  loadOlderMessages() {
    const oldest = this.messages()[0];
    if (!oldest || this.loadingMore()) return;

    this.loadingMore.set(true);

    const container = this.chatScrollRef?.nativeElement;
    const prevScrollHeight = container?.scrollHeight ?? 0;

    this.chatService.getByBooking(this.id(), oldest.createdAt).subscribe({
      next: (result) => {
        this.messages.update((list) => [...result.items, ...list]);
        this.hasMoreMessages.set(result.hasMore);
        this.loadingMore.set(false);

        setTimeout(() => {
          if (!container) return;
          container.scrollTop = container.scrollHeight - prevScrollHeight;
        });
      },
      error: () => {
        this.loadingMore.set(false);
      },
    });
  }

  onChatScroll(event: Event) {
    const el = event.target as HTMLDivElement;

    if (el.scrollTop < 60 && this.hasMoreMessages() && !this.loadingMore()) {
      this.loadOlderMessages();
    }
  }

  private scrollToBottom() {
    const el = this.chatScrollRef?.nativeElement;
    if (el) el.scrollTop = el.scrollHeight;
  }

  isMine(m: ChatMessage): boolean {
    if (this.isAdmin()) return m.senderRole === 'Admin';
    if (this.isAgent()) return m.senderRole === 'Agent';
    return m.senderRole === 'Patient';
  }

  initials(role: string): string {
    return role.charAt(0).toUpperCase();
  }

  sendMessage() {
    const content = this.newMessage().trim();
    if (!content) return;

    this.loading.set(true);

    this.chatService.send(this.id(), content).subscribe({
      next: (msg) => {
        this.messages.update((list) => {
          if (list.some((m) => m.id === msg.id)) return list;
          return [...list, msg];
        });

        this.newMessage.set('');
        this.loading.set(false);
        setTimeout(() => this.scrollToBottom());
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
        this.documents.update((list) => {
          if (list.some((d) => d.id === doc.id)) return list;
          return [doc, ...list];
        });
        this.loading.set(false);
        input.value = '';
      },
      error: () => {
        this.message.set('Upload failed');
        this.loading.set(false);
        input.value = '';
      },
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
      next: (b) => {
        this.booking.set(b);
        this.message.set('Booking accepted');
      },
      error: (e) => this.message.set(e.error || 'Failed'),
    });
  }

  decline() {
    this.bookingService.decline(this.id()).subscribe({
      next: (b) => {
        this.booking.set(b);
        this.message.set('Booking declined');
      },
      error: (e) => this.message.set(e.error || 'Failed'),
    });
  }

  refer() {
    const agentId = this.selectedAgentId();
    if (!agentId) {
      this.message.set('Select an agent');
      return;
    }

    this.bookingService.refer(this.id(), agentId).subscribe({
      next: (b) => {
        this.booking.set(b);
        this.message.set('Referred to another agent');
      },
      error: (e) => this.message.set(e.error || 'Failed'),
    });
  }

  downloadDocument(doc: DocumentItem) {
    this.documentService.download(doc.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = doc.fileName;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => this.message.set('Download failed'),
    });
  }

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