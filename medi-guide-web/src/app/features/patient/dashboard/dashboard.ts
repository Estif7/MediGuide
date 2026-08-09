import { Component, inject, signal, OnInit } from '@angular/core';
import { AuthService } from '../../../core/services/auth';
import { CategoryService } from '../../../core/services/category';
import { BookingService } from '../../../core/services/booking';
import { ServiceCategory } from '../../../core/models/service-category.model';
import { Booking } from '../../../core/models/booking.model';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { bookingStatusLabel } from '../../../core/utils/status-label';

@Component({
  selector: 'app-patient-dashboard',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly categoryService = inject(CategoryService);
  private readonly bookingService = inject(BookingService);

  user = this.auth.currentUser;
  categories = signal<ServiceCategory[]>([]);
  bookings = signal<Booking[]>([]);
  selectedCategoryId = signal<string>('');
  notes = signal('');
  message = signal<string | null>(null);
  loading = signal(false);
  statusLabel = bookingStatusLabel;

  ngOnInit() {
    this.loadCategories();
    this.loadBookings();
  }

  loadCategories() {
    this.categoryService.getAll().subscribe({
      next: (data) => this.categories.set(data),
      error: () => this.message.set('Failed to load categories'),
    });
  }

  loadBookings() {
    this.bookingService.getAll().subscribe({
      next: (data) => {
        const patientId = this.user()?.patientId;
        const mine = patientId
          ? data.filter((b) => b.patientId === patientId)
          : data;
        this.bookings.set(mine);
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
        responseTime: 2, // Standard for now
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