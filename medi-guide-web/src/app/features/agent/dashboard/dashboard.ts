import { Component, inject, signal, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth';
import { UserService } from '../../../core/services/user';
import { BookingService } from '../../../core/services/booking';
import { TranslationService } from '../../../core/services/translation';
import { FeedbackService } from '../../../core/services/feedback.service';
import { StatePanel } from '../../../shared/state-panel/state-panel';
import { Booking } from '../../../core/models/booking.model';
import { bookingStatusLabel } from '../../../core/utils/status-label';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-agent-dashboard',
  standalone: true,
  imports: [
    RouterLink,
    MatCardModule,
    MatListModule,
    MatButtonModule,
    StatePanel,
    MatIconModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly bookingService = inject(BookingService);
  private readonly feedback = inject(FeedbackService);
  readonly i18n = inject(TranslationService);

  user = this.auth.currentUser;
  bookings = signal<Booking[]>([]);
  bookingsLoading = signal(false);
  bookingsError = signal<string | null>(null);

  isAvailable = signal<boolean>(true);
  updatingAvailability = signal<boolean>(false);

  statusLabel = (s: number) => bookingStatusLabel(s, this.i18n.isAmharic());

  ngOnInit() {
    this.loadBookings();
    this.loadAvailability();
  }

  loadAvailability() {
    this.userService.getMe().subscribe({
      next: (profile) => {
        this.isAvailable.set(profile.isAvailable ?? true);
      },
      error: () => {},
    });
  }

  toggleAvailability() {
    const current = this.isAvailable();
    const next = !current;
    this.updatingAvailability.set(true);
    this.isAvailable.set(next);

    this.userService
      .updateMe({
        fullName: this.user()?.fullName || 'Healthcare Professional',
        isAvailable: next,
      })
      .subscribe({
        next: (updated) => {
          this.isAvailable.set(updated.isAvailable ?? next);
          this.updatingAvailability.set(false);
          const msg = next
            ? (this.i18n.isAmharic()
                ? 'የህክምና ተቀባይነት ሁኔታዎ አሁን ንቁ (ክፍት) ሆኗል'
                : 'Intake status set to Active: accepting new patient consultations')
            : (this.i18n.isAmharic()
                ? 'የህክምና ተቀባይነት ሁኔታዎ አሁን ስራ ላይ / እረፍት ላይ ተቀይሯል'
                : 'Intake status set to Inactive: currently not taking new consultations');
          this.feedback.success(msg);
        },
        error: () => {
          this.isAvailable.set(current);
          this.updatingAvailability.set(false);
          const err = this.i18n.isAmharic()
            ? 'ሁኔታውን መቀየር አልተቻለም'
            : 'Failed to update availability status';
          this.feedback.error(err);
        },
      });
  }

  loadBookings() {
    this.bookingsLoading.set(true);
    this.bookingsError.set(null);

    this.bookingService.getAll().subscribe({
      next: (data) => {
        this.bookings.set(data.items);
        this.bookingsLoading.set(false);
      },
      error: () => {
        this.bookingsError.set(
          this.i18n.isAmharic()
            ? 'የምክክር ጥያቄዎችን መጫን አልተቻለም'
            : 'Failed to load bookings'
        );
        this.bookingsLoading.set(false);
      },
    });
  }

  logout() {
    this.auth.logout();
  }
}