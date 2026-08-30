import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth';
import { BookingService } from '../../../core/services/booking';
import { ReferenceDataService } from '../../../core/services/reference-data';
import { Booking } from '../../../core/models/booking.model';
import { bookingStatusLabel } from '../../../core/utils/status-label';

@Component({
  selector: 'app-patient-dashboard',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatListModule,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly bookingService = inject(BookingService);
  private readonly referenceData = inject(ReferenceDataService);

  user = this.auth.currentUser;
  categories = this.referenceData.categories;
  bookings = signal<Booking[]>([]);
  selectedCategoryId = signal<string>('');
  notes = signal('');
  message = signal<string | null>(null);
  loading = signal(false);
  statusLabel = bookingStatusLabel;

  ngOnInit() {
    this.referenceData.loadCategories();
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

  createBooking() {
    const patientId = this.user()?.patientId;
    const categoryId = this.selectedCategoryId();

    if (!patientId || !categoryId) {
      this.message.set('Please select a service');
      return;
    }

    this.loading.set(true);
    this.message.set(null);

    this.bookingService
      .create({
        patientId,
        serviceCategoryId: categoryId,
        responseTime: 2,
        notes: this.notes() || undefined,
      })
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.message.set('Booking created successfully');
          this.notes.set('');
          this.loadBookings();
        },
        error: () => {
          this.loading.set(false);
          this.message.set('Failed to create booking');
        },
      });
  }

  logout() {
    this.auth.logout();
  }
}