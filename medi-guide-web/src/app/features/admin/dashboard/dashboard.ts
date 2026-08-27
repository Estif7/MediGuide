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
import { AgentService, AgentDto } from '../../../core/services/agent';
import { Booking, BookingStatus } from '../../../core/models/booking.model';
import { bookingStatusLabel } from '../../../core/utils/status-label';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    MatToolbarModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatListModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly bookingService = inject(BookingService);
  private readonly patientService = inject(PatientService);
  private readonly agentService = inject(AgentService);

  user = this.auth.currentUser;

  bookings = signal<Booking[]>([]);
  totalCount = signal(0);
  page = signal(1);
  pageSize = 10;
  statusFilter = signal<BookingStatus | null>(null);

  patients = signal<PatientDto[]>([]);
  agents = signal<AgentDto[]>([]);
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

  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));

  agentName = signal('');
  agentEmail = signal('');
  agentPhone = signal('');
  agentPassword = signal('');

  ngOnInit() {
    this.reload();
  }

  reload() {
    this.loadBookings();

    this.patientService.getAll().subscribe({
      next: (d) => this.patients.set(d),
      error: () => this.message.set('Failed to load patients'),
    });
    this.agentService.getAll().subscribe({
      next: (d) => this.agents.set(d),
      error: () => this.message.set('Failed to load agents'),
    });
  }

  loadBookings() {
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
        },
        error: () => this.message.set('Failed to load bookings'),
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
      this.message.set('Name, email and password are required');
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
          this.message.set('Agent registered successfully');
          this.agentName.set('');
          this.agentEmail.set('');
          this.agentPhone.set('');
          this.agentPassword.set('');
          this.reload();
        },
        error: (err) => {
          this.loading.set(false);
          this.message.set(err.error?.[0] || err.error || 'Failed to register agent');
        },
      });
  }

  logout() {
    this.auth.logout();
  }
}