import { Component, inject, signal, OnInit, computed } from '@angular/core';
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
import { TranslationService } from '../../../core/services/translation';
import { StatePanel } from '../../../shared/state-panel/state-panel';
import { Booking, ResponseTime } from '../../../core/models/booking.model';
import { bookingStatusLabel, responseTimeLabel } from '../../../core/utils/status-label';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

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
    MatProgressSpinnerModule,
    StatePanel,
    MatIconModule,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly bookingService = inject(BookingService);
  readonly referenceData = inject(ReferenceDataService);
  readonly i18n = inject(TranslationService);

  user = this.auth.currentUser;
  categories = this.referenceData.categories;
  categoriesLoading = this.referenceData.categoriesLoading;
  categoriesError = this.referenceData.categoriesError;

  bookings = signal<Booking[]>([]);
  bookingsLoading = signal(false);
  bookingsError = signal<string | null>(null);

  selectedCategoryId = signal<string>('');
  selectedResponseTime = signal<ResponseTime>(2); // Default Standard (5 days)
  notes = signal('');
  message = signal<string | null>(null);
  loading = signal(false);
  statusLabel = (s: number) => bookingStatusLabel(s, this.i18n.isAmharic());
  timeLabel = (r: number) => responseTimeLabel(r, this.i18n.isAmharic());

  selectedCategory = computed(() =>
    this.categories().find((c) => c.id === this.selectedCategoryId())
  );

  estimatedPrice = computed(() => {
    const cat = this.selectedCategory();
    if (!cat) return 0;
    const mult = this.selectedResponseTime() === 0 ? 1.75 : this.selectedResponseTime() === 1 ? 1.30 : 1.0;
    return Math.round(cat.basePrice * mult);
  });

  ngOnInit() {
    this.referenceData.loadCategories();
    this.loadBookings();
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
        this.bookingsError.set('Failed to load bookings');
        this.bookingsLoading.set(false);
      },
    });
  }

  createBooking() {
    const patientId = this.user()?.patientId;
    const categoryId = this.selectedCategoryId();

    if (!patientId || !categoryId) {
      this.message.set(this.i18n.isAmharic() ? 'እባክዎ አገልግሎት ይምረጡ' : 'Please select a service');
      return;
    }

    this.loading.set(true);
    this.message.set(null);

    this.bookingService
      .create({
        patientId,
        serviceCategoryId: categoryId,
        responseTime: this.selectedResponseTime(),
        notes: this.notes() || undefined,
      })
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.message.set(this.i18n.isAmharic() ? 'ምክክርዎ በተሳካ ሁኔታ ተመዝግቧል' : 'Booking created successfully');
          this.notes.set('');
          this.loadBookings();
        },
        error: () => {
          this.loading.set(false);
          this.message.set(this.i18n.isAmharic() ? 'ምዝገባው አልተሳካም' : 'Failed to create booking');
        },
      });
  }

  logout() {
    this.auth.logout();
  }
}