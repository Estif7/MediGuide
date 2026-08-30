import { Component, inject, signal, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth';
import { BookingService } from '../../../core/services/booking';
import { Booking } from '../../../core/models/booking.model';
import { bookingStatusLabel } from '../../../core/utils/status-label';

@Component({
  selector: 'app-agent-dashboard',
  standalone: true,
  imports: [RouterLink, MatCardModule, MatListModule, MatButtonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly bookingService = inject(BookingService);

  user = this.auth.currentUser;
  bookings = signal<Booking[]>([]);
  message = signal<string | null>(null);
  statusLabel = bookingStatusLabel;

  ngOnInit() {
    this.loadBookings();
  }

  loadBookings() {
    this.bookingService.getAll().subscribe({
      next: (data) => {
        this.bookings.set(data.items);
      },
      error: () => this.message.set('Failed to load bookings'),
    });
  }

  logout() {
    this.auth.logout();
  }
}