import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth';
import { BookingService } from '../../../core/services/booking';
import { PatientService, PatientDto } from '../../../core/services/patient';
import { AgentService, AgentDto } from '../../../core/services/agent';
import { Booking } from '../../../core/models/booking.model';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [FormsModule, RouterLink],
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
  patients = signal<PatientDto[]>([]);
  agents = signal<AgentDto[]>([]);
  message = signal<string | null>(null);
  loading = signal(false);

  // Register agent form fields
  agentName = signal('');
  agentEmail = signal('');
  agentPhone = signal('');
  agentPassword = signal('');

  ngOnInit() {
    this.reload();
  }

  reload() {
    this.bookingService.getAll().subscribe({
      next: (d) => this.bookings.set(d),
      error: () => this.message.set('Failed to load bookings'),
    });
    this.patientService.getAll().subscribe({
      next: (d) => this.patients.set(d),
      error: () => this.message.set('Failed to load patients'),
    });
    this.agentService.getAll().subscribe({
      next: (d) => this.agents.set(d),
      error: () => this.message.set('Failed to load agents'),
    });
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