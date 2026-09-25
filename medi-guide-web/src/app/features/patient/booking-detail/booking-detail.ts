import {
  Component,
  inject,
  signal,
  computed,
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

import { Router, RouterLink } from '@angular/router';
import { Location } from '@angular/common';

import {
  bookingStatusLabel,
  responseTimeLabel,
} from '../../../core/utils/status-label';

import { InternalNoteService } from '../../../core/services/internal-note';
import { TestimonialService } from '../../../core/services/testimonial';
import { TranslationService } from '../../../core/services/translation';
import { FeedbackService } from '../../../core/services/feedback.service';
import { InternalNote } from '../../../core/models/internal-note.model';

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

  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly bookingService = inject(BookingService);
  private readonly chatService = inject(ChatService);
  private readonly documentService = inject(DocumentService);
  private readonly referenceData = inject(ReferenceDataService);
  private readonly auth = inject(AuthService);
  private readonly signalR = inject(SignalRService);
  private readonly internalNoteService = inject(InternalNoteService);
  private readonly testimonialService = inject(TestimonialService);
  private readonly feedback = inject(FeedbackService);
  readonly i18n = inject(TranslationService);

  readonly backUrl = computed(() => {
    if (this.isPatient()) return '/patient';
    if (this.isAgent()) return '/agent';
    if (this.isAdmin()) return '/admin';
    return '/';
  });

  goBack() {
    this.router.navigateByUrl(this.backUrl());
  }

  @ViewChild('chatScroll')
  chatScrollRef?: ElementRef<HTMLDivElement>;

  agents = this.referenceData.agents;
  selectedAgentId = signal('');

  isAdmin = this.auth.isAdmin;
  isAgent = this.auth.isAgent;
  isPatient = this.auth.isPatient;

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

  // Clinical internal notes for agents & admins
  internalNotes = signal<InternalNote[]>([]);
  internalNotesLoading = signal(false);
  newInternalNote = signal('');
  savingNote = signal(false);

  // Patient review submission
  reviewRating = signal<number>(5);
  reviewComment = signal('');
  submittingReview = signal(false);
  reviewSubmitted = signal(false);

  newMessage = signal('');
  message = signal<string | null>(null); // success/info toasts only now
  loading = signal(false);

  statusLabel = (s: number) => bookingStatusLabel(s, this.i18n.isAmharic());
  timeLabel = (r: number) => responseTimeLabel(r, this.i18n.isAmharic());

  allowedStatuses: BookingStatus[] = [0, 1, 2, 3, 4, 5];

  currentAgentId = computed(() => this.auth.currentUser()?.agentId);
  isMyAssignedBooking = computed(() => {
    const myId = this.currentAgentId();
    const b = this.booking();
    return !!(myId && b && b.agentId === myId);
  });
  canAcceptBooking = computed(() => {
    const b = this.booking();
    return this.isMyAssignedBooking() && b?.status === 2 && !b?.isReferralPendingApproval;
  });
  canDeclineBooking = computed(() => {
    const b = this.booking();
    return this.isMyAssignedBooking() && b?.status === 2 && !b?.isReferralPendingApproval;
  });
  canCompleteBooking = computed(() => {
    const b = this.booking();
    return (this.isAdmin() || this.isMyAssignedBooking()) && (b?.status === 2 || b?.status === 3) && !b?.isReferralPendingApproval;
  });
  canReferBooking = computed(() => {
    const b = this.booking();
    return (this.isAdmin() || this.isMyAssignedBooking()) && (b?.status === 2 || b?.status === 3) && !b?.isReferralPendingApproval;
  });
  canAdminAssign = computed(() => {
    const b = this.booking();
    return this.isAdmin() && (b?.status === 1 || b?.status === 2 || b?.status === 3) && !b?.isReferralPendingApproval;
  });

  // Simulated payment state
  showPaymentDialog = signal(false);
  selectedPaymentMethod = signal<'Telebirr' | 'CBE Birr' | 'Chapa Test'>('Telebirr');
  paymentPhoneNumber = signal('0911223344');
  simulatingPayment = signal(false);

  // Clinical referral state
  showReferDialog = signal(false);
  referTargetAgentId = signal('');
  referReasonType = signal('Specialist Clinical Competence Required');
  referClinicalNotes = signal('');
  submittingReferral = signal(false);
  selectedReferralDepartment = signal<string>('all');

  // Admin referral approval state
  approvingReferral = signal(false);
  rejectingReferral = signal(false);
  rejectReasonInput = signal('');
  showRejectDialog = signal(false);

  availableReferralAgents = computed(() => {
    const myId = this.currentAgentId();
    return this.agents().filter((a) => !myId || a.id !== myId);
  });

  referralDepartments = computed(() => {
    const set = new Set<string>();
    for (const a of this.availableReferralAgents()) {
      if (a.department) set.add(a.department);
    }
    return Array.from(set).sort();
  });

  filteredReferralAgents = computed(() => {
    const dept = this.selectedReferralDepartment();
    const list = this.availableReferralAgents();
    if (dept === 'all') return list;
    return list.filter((a) => a.department === dept);
  });

  selectedTargetSpecialist = computed(() => {
    const id = this.referTargetAgentId();
    return this.agents().find((a) => a.id === id) || null;
  });

  ngOnInit() {
    this.loadBooking();
    this.loadMessages();
    this.loadDocuments();
    this.referenceData.loadAgents();

    if (this.isAdmin() || this.isAgent()) {
      this.loadInternalNotes();
    }

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

    this.signalR.onInternalNoteAdded((note) => {
      if (note.bookingId !== this.id()) return;
      if (this.internalNotes().some((n) => n.id === note.id)) return;
      this.internalNotes.update((list) => [...list, note]);
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
    this.signalR.offInternalNoteAdded();
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

  isHealthcareProfessional(m: ChatMessage): boolean {
    return m.senderRole === 'Agent';
  }

  getSenderDisplayName(m: ChatMessage): string {
    if (m.senderRole === 'Agent') {
      if (m.senderName && m.senderName.toLowerCase() !== 'agent') {
        return m.senderName;
      }
      const assigned = this.booking()?.agentName;
      if (assigned) return assigned;
      return this.i18n.isAmharic() ? 'የጤና ባለሙያ' : 'Healthcare Professional';
    }
    if (m.senderRole === 'Admin') {
      if (m.senderName && m.senderName.toLowerCase() !== 'admin') {
        return m.senderName;
      }
      return this.i18n.isAmharic() ? 'አስተዳዳሪ' : 'Administrator';
    }
    if (m.senderName && m.senderName.toLowerCase() !== 'patient') {
      return m.senderName;
    }
    const patient = this.booking()?.patientName;
    if (patient) return patient;
    return this.i18n.isAmharic() ? 'ታካሚ' : 'Patient';
  }

  getSenderInitials(m: ChatMessage): string {
    const name = this.getSenderDisplayName(m);
    if (!name) return '?';
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0].charAt(0) + parts[1].charAt(0)).toUpperCase();
    }
    return parts[0].substring(0, 2).toUpperCase();
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
        this.feedback.error('Failed to send message');
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
        this.feedback.success(this.i18n.isAmharic() ? 'ሰነዱ በተሳካ ሁኔታ ተጭኗል' : 'Document uploaded successfully');
        input.value = '';
      },
      error: () => {
        this.feedback.error('Upload failed');
        this.loading.set(false);
        input.value = '';
      },
    });
  }

  assignAgent() {
    const agentId = this.selectedAgentId();
    if (!agentId) {
      this.feedback.error(this.i18n.isAmharic() ? 'እባክዎ የጤና ባለሙያ ይምረጡ' : 'Select a Healthcare Professional');
      return;
    }

    this.loading.set(true);

    this.bookingService.assignAgent(this.id(), agentId).subscribe({
      next: (updated) => {
        this.booking.set(updated);
        const successMsg = this.i18n.isAmharic()
          ? 'የጤና ባለሙያ በተሳካ ሁኔታ ተመድቧል'
          : 'Healthcare Professional assigned successfully';
        this.feedback.success(successMsg);
        this.message.set(successMsg);
        this.loading.set(false);
      },
      error: (err) => {
        const errorMsg = err.error || 'Assign failed';
        this.feedback.error(errorMsg);
        this.message.set(errorMsg);
        this.loading.set(false);
      },
    });
  }

  simulatePayment() {
    this.simulatingPayment.set(true);
    this.bookingService
      .simulatePayment(this.id(), {
        paymentMethod: this.selectedPaymentMethod(),
        transactionReference: `SIM-${Date.now()}`,
      })
      .subscribe({
        next: (b) => {
          this.booking.set(b);
          this.simulatingPayment.set(false);
          this.showPaymentDialog.set(false);
          const payMsg = this.i18n.isAmharic()
            ? `ክፍያው በ${this.selectedPaymentMethod()} በተሳካ ሁኔታ ተረጋግጧል! ጥያቄዎ ወደ ባለሙያ ምደባ ተላልፏል።`
            : `Payment confirmed via ${this.selectedPaymentMethod()}! Request queued for specialist triage.`;
          this.feedback.success(payMsg);
          this.message.set(payMsg);
        },
        error: (err) => {
          this.simulatingPayment.set(false);
          const errText = err.error || 'Payment simulation failed';
          this.feedback.error(errText);
          this.message.set(errText);
        },
      });
  }

  openReferDialog() {
    this.referTargetAgentId.set('');
    this.selectedReferralDepartment.set('all');
    this.referReasonType.set('Specialist Clinical Competence Required');
    this.referClinicalNotes.set('');
    this.referenceData.refreshAgents();
    this.showReferDialog.set(true);
  }

  closeReferDialog() {
    this.showReferDialog.set(false);
  }

  submitReferral() {
    const targetAgentId = this.referTargetAgentId();
    if (!targetAgentId) {
      this.feedback.error(this.i18n.isAmharic() ? 'እባክዎ ባለሙያ ይምረጡ' : 'Please select a target specialist');
      return;
    }

    this.submittingReferral.set(true);
    this.bookingService
      .refer(this.id(), {
        targetAgentId,
        reason: this.referReasonType(),
        clinicalNotes: this.referClinicalNotes().trim() || undefined,
      })
      .subscribe({
        next: (b) => {
          this.booking.set(b);
          this.submittingReferral.set(false);
          this.showReferDialog.set(false);
          const refMsg = this.isAdmin()
            ? (this.i18n.isAmharic() ? 'የህክምና ምክክሩ በቀጥታ ተላልፏል' : 'Consultation transferred directly by Admin.')
            : (this.i18n.isAmharic()
                ? 'የሪፈራል ጥያቄው ተልኳል፤ በአስተዳዳሪው ሲጸድቅ ርክክቡ ይጠናቀቃል'
                : 'Referral handover submitted. Consultation is awaiting Admin approval before the receiving specialist can proceed.');
          this.feedback.success(refMsg);
          this.message.set(refMsg);
          this.loadInternalNotes();
        },
        error: (err) => {
          this.submittingReferral.set(false);
          const errText = err.error || 'Referral failed';
          this.feedback.error(errText);
          this.message.set(errText);
        },
      });
  }

  approveReferral() {
    this.approvingReferral.set(true);
    this.bookingService.approveReferral(this.id()).subscribe({
      next: (b) => {
        this.booking.set(b);
        this.approvingReferral.set(false);
        const appMsg = this.i18n.isAmharic()
          ? 'የክሊኒካል ሪፈራሉ በአስተዳዳሪ ጸድቋል፤ ጉዳዩ ወደ አዲሱ ስፔሻሊስት ተላልፏል'
          : 'Clinical referral approved by Admin. Case transferred to receiving specialist.';
        this.feedback.success(appMsg);
        this.message.set(appMsg);
        this.loadInternalNotes();
      },
      error: (err) => {
        this.approvingReferral.set(false);
        const errText = err.error || 'Failed to approve referral';
        this.feedback.error(errText);
        this.message.set(errText);
      },
    });
  }

  openRejectDialog() {
    this.rejectReasonInput.set('');
    this.showRejectDialog.set(true);
  }

  closeRejectDialog() {
    this.showRejectDialog.set(false);
  }

  confirmRejectReferral() {
    this.rejectingReferral.set(true);
    this.bookingService.rejectReferral(this.id(), this.rejectReasonInput().trim() || undefined).subscribe({
      next: (b) => {
        this.booking.set(b);
        this.rejectingReferral.set(false);
        this.showRejectDialog.set(false);
        const decMsg = this.i18n.isAmharic()
          ? 'የሪፈራል ጥያቄው ውድቅ ተደርጓል። ጉዳዩ አሁንም በቀድሞው ባለሙያ እጅ ይቆያል'
          : 'Referral request declined. Case remains assigned to current specialist.';
        this.feedback.info(decMsg);
        this.message.set(decMsg);
        this.loadInternalNotes();
      },
      error: (err) => {
        this.rejectingReferral.set(false);
        const errText = err.error || 'Failed to reject referral';
        this.feedback.error(errText);
        this.message.set(errText);
      },
    });
  }

  accept() {
    this.loading.set(true);
    this.bookingService.accept(this.id()).subscribe({
      next: (b) => {
        this.booking.set(b);
        const accMsg = this.i18n.isAmharic() ? 'የምክክር ጥያቄውን ተቀብለዋል' : 'Consultation accepted';
        this.feedback.success(accMsg);
        this.message.set(accMsg);
        this.loading.set(false);
      },
      error: (e) => {
        const errText = e.error || 'Failed to accept booking';
        this.feedback.error(errText);
        this.message.set(errText);
        this.loading.set(false);
      },
    });
  }

  complete() {
    this.loading.set(true);
    this.bookingService.complete(this.id()).subscribe({
      next: (b) => {
        this.booking.set(b);
        const compMsg = this.i18n.isAmharic() ? 'የህክምና ምክክሩ በተሳካ ሁኔታ ተጠናቋል' : 'Consultation marked as completed';
        this.feedback.success(compMsg);
        this.message.set(compMsg);
        this.loading.set(false);
      },
      error: (e) => {
        const errText = e.error || 'Failed to complete consultation';
        this.feedback.error(errText);
        this.message.set(errText);
        this.loading.set(false);
      },
    });
  }

  decline() {
    this.bookingService.decline(this.id()).subscribe({
      next: (b) => {
        this.booking.set(b);
        const decMsg = this.i18n.isAmharic() ? 'ምደባው አልተቀበሉትም' : 'Booking declined';
        this.feedback.info(decMsg);
        this.message.set(decMsg);
      },
      error: (e) => {
        const errText = e.error || 'Failed to decline';
        this.feedback.error(errText);
        this.message.set(errText);
      },
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
      error: () => this.feedback.error('Download failed'),
    });
  }

  loadInternalNotes() {
    this.internalNotesLoading.set(true);
    this.internalNoteService.getByBooking(this.id()).subscribe({
      next: (notes) => {
        this.internalNotes.set(notes);
        this.internalNotesLoading.set(false);
      },
      error: () => this.internalNotesLoading.set(false),
    });
  }

  saveInternalNote() {
    const content = this.newInternalNote().trim();
    if (!content) return;

    this.savingNote.set(true);
    this.internalNoteService.create(this.id(), content).subscribe({
      next: (note) => {
        this.internalNotes.update((list) => {
          if (list.some((n) => n.id === note.id)) return list;
          return [...list, note];
        });
        this.newInternalNote.set('');
        this.savingNote.set(false);
        const noteMsg = this.i18n.isAmharic() ? 'የውስጥ ማስታወሻ ተቀምጧል' : 'Internal clinical note saved';
        this.feedback.success(noteMsg);
        this.message.set(noteMsg);
      },
      error: () => {
        this.savingNote.set(false);
        this.feedback.error('Failed to save internal note');
      },
    });
  }

  submitReview() {
    const comment = this.reviewComment().trim();
    if (!comment) return;

    this.submittingReview.set(true);
    this.testimonialService.submit({
      rating: this.reviewRating(),
      comment,
      bookingId: this.id(),
    }).subscribe({
      next: () => {
        this.submittingReview.set(false);
        this.reviewSubmitted.set(true);
        const revMsg = this.i18n.isAmharic() ? 'አስተያየትዎ እናደርሰዋለን፣ እናመሰግናለን!' : 'Thank you! Your review has been submitted for verification.';
        this.feedback.success(revMsg);
        this.message.set(revMsg);
      },
      error: () => {
        this.submittingReview.set(false);
        this.feedback.error('Failed to submit review');
      },
    });
  }

  updateStatus(newStatus: BookingStatus) {
    this.bookingService.updateBookingStatus(this.id(), newStatus).subscribe({
      next: (updated) => {
        this.booking.set(updated);
        const statMsg = `Status updated to ${this.statusLabel(newStatus)}`;
        this.feedback.success(statMsg);
        this.message.set(statMsg);
      },
      error: (err) => {
        const errText = err.error?.title || 'Failed to update status';
        this.feedback.error(errText);
        this.message.set(errText);
      },
    });
  }
}