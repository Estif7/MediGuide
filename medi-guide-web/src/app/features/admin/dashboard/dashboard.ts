import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth';
import { BookingService } from '../../../core/services/booking';
import { PatientService, PatientDto } from '../../../core/services/patient';
import { AgentService } from '../../../core/services/agent';
import { ReferenceDataService } from '../../../core/services/reference-data';
import { TestimonialService } from '../../../core/services/testimonial';
import { TranslationService } from '../../../core/services/translation';
import { FeedbackService } from '../../../core/services/feedback.service';
import { AdminTestimonial } from '../../../core/models/testimonial.model';
import { StatePanel } from '../../../shared/state-panel/state-panel';
import { Booking, BookingStatus } from '../../../core/models/booking.model';
import { bookingStatusLabel } from '../../../core/utils/status-label';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    FormsModule,
    MatIconModule,
    RouterLink,
    MatToolbarModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatListModule,
    MatProgressSpinnerModule,
    StatePanel,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly bookingService = inject(BookingService);
  private readonly patientService = inject(PatientService);
  private readonly agentService = inject(AgentService);
  private readonly testimonialService = inject(TestimonialService);
  private readonly feedback = inject(FeedbackService);
  readonly i18n = inject(TranslationService);

  // Public because the template needs access to refreshAgents().
  readonly referenceData = inject(ReferenceDataService);

  user = this.auth.currentUser;

  bookings = signal<Booking[]>([]);
  bookingsLoading = signal(false);
  bookingsError = signal<string | null>(null);
  totalCount = signal(0);
  page = signal(1);
  pageSize = 10;
  statusFilter = signal<BookingStatus | null>(null);

  patientsTotalCount = signal(0);
  patients = signal<PatientDto[]>([]);
  patientsLoading = signal(false);
  patientsError = signal<string | null>(null);

  // Agent state is owned by ReferenceDataService.
  agents = this.referenceData.agents;
  agentsTotalCount = computed(() => this.agents().length);
  agentsLoading = this.referenceData.agentsLoading;
  agentsError = this.referenceData.agentsError;

  // Show at most 5 at a time on dashboard overview
  displayedAgents = computed(() => this.agents().slice(0, 5));
  displayedPatients = computed(() => this.patients().slice(0, 5));

  // Testimonials moderation queue
  testimonials = signal<AdminTestimonial[]>([]);
  testimonialsLoading = signal(false);

  message = signal<string | null>(null);
  loading = signal(false);
  statusLabel = bookingStatusLabel;

  statusOptions: { value: BookingStatus | null; label: string }[] = [
    { value: null, label: 'All statuses' },
    { value: 0, label: bookingStatusLabel(0) },
    { value: 1, label: bookingStatusLabel(1) },
    { value: 2, label: bookingStatusLabel(2) },
    { value: 3, label: bookingStatusLabel(3) },
    { value: 4, label: bookingStatusLabel(4) },
    { value: 5, label: bookingStatusLabel(5) },
  ];

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize)),
  );

  agentName = signal('');
  agentEmail = signal('');
  agentPhone = signal('');
  agentPassword = signal('');

  ngOnInit() {
    this.reload();
  }

  reload() {
    this.loadBookings();
    this.referenceData.loadAgents();
    this.loadPatients();
    this.loadTestimonials();
  }

  loadBookings() {
    this.bookingsLoading.set(true);
    this.bookingsError.set(null);

    this.bookingService
      .getAll({
        page: this.page(),
        pageSize: this.pageSize,
        status: this.statusFilter() ?? undefined,
        sortBy: 'CreatedAt',
        sortDir: 'desc',
      })
      .subscribe({
        next: (d) => {
          this.bookings.set(d.items);
          this.totalCount.set(d.totalCount);
          this.bookingsLoading.set(false);
        },
        error: () => {
          this.bookingsError.set('Failed to load bookings');
          this.bookingsLoading.set(false);
        },
      });
  }

  loadPatients() {
    this.patientsLoading.set(true);
    this.patientsError.set(null);

    this.patientService.getAll().subscribe({
      next: (d) => {
        this.patients.set(d.items);
        this.patientsTotalCount.set(d.totalCount);
        this.patientsLoading.set(false);
      },
      error: () => {
        this.patientsError.set('Failed to load patients');
        this.patientsLoading.set(false);
      },
    });
  }

  onStatusFilterChange(value: BookingStatus | null) {
    this.statusFilter.set(value);
    this.page.set(1);
    this.loadBookings();
  }

  nextPage() {
    if (this.page() < this.totalPages()) {
      this.page.set(this.page() + 1);
      this.loadBookings();
    }
  }

  prevPage() {
    if (this.page() > 1) {
      this.page.set(this.page() - 1);
      this.loadBookings();
    }
  }

  registerAgent() {
    if (!this.agentName() || !this.agentEmail() || !this.agentPassword()) {
      const msg = 'Name, email and password are required';
      this.feedback.error(msg);
      this.message.set(msg);
      return;
    }

    this.loading.set(true);
    this.message.set(null);

    this.agentService
      .register({
        fullName: this.agentName(),
        email: this.agentEmail(),
        phoneNumber: this.agentPhone() || '',
        password: this.agentPassword(),
      })
      .subscribe({
        next: () => {
          this.loading.set(false);
          const successMsg = 'Healthcare Professional registered successfully';
          this.feedback.success(successMsg);
          this.message.set(successMsg);

          this.agentName.set('');
          this.agentEmail.set('');
          this.agentPhone.set('');
          this.agentPassword.set('');

          this.referenceData.refreshAgents();
          this.loadPatients();
        },
        error: (err) => {
          this.loading.set(false);
          const errMsg = err.error?.[0] || err.error || 'Failed to register healthcare professional';
          this.feedback.error(errMsg);
          this.message.set(errMsg);
        },
      });
  }

  loadTestimonials() {
    this.testimonialsLoading.set(true);
    this.testimonialService.getForAdmin().subscribe({
      next: (data) => {
        this.testimonials.set(data);
        this.testimonialsLoading.set(false);
      },
      error: () => this.testimonialsLoading.set(false),
    });
  }

  approveTestimonial(id: string) {
    this.testimonialService.approve(id).subscribe({
      next: () => {
        this.testimonials.update((list) =>
          list.map((t) => (t.id === id ? { ...t, isApproved: true } : t))
        );
        const msg = 'Testimonial approved and published to homepage.';
        this.feedback.success(msg);
        this.message.set(msg);
      },
      error: () => {
        const err = 'Failed to approve testimonial';
        this.feedback.error(err);
        this.message.set(err);
      },
    });
  }

  deleteTestimonial(id: string) {
    this.testimonialService.delete(id).subscribe({
      next: () => {
        this.testimonials.update((list) => list.filter((t) => t.id !== id));
        const msg = 'Testimonial removed.';
        this.feedback.info(msg);
        this.message.set(msg);
      },
      error: () => {
        const err = 'Failed to delete testimonial';
        this.feedback.error(err);
        this.message.set(err);
      },
    });
  }

  logout() {
    this.auth.logout();
  }
}
