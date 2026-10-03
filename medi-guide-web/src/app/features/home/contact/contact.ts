import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslationService } from '../../../core/services/translation';
import { ThemeService } from '../../../core/services/theme';
import { AuthService } from '../../../core/services/auth';
import { FeedbackService } from '../../../core/services/feedback.service';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './contact.html',
  styleUrl: './contact.scss',
})
export class Contact {
  readonly i18n = inject(TranslationService);
  readonly themeService = inject(ThemeService);
  private readonly auth = inject(AuthService);
  private readonly feedback = inject(FeedbackService);

  isLoggedIn = this.auth.isLoggedIn;

  fullName = signal('');
  email = signal('');
  phone = signal('');
  inquiryType = signal('General Consultation Inquiry');
  message = signal('');
  submitting = signal(false);
  submitted = signal(false);

  inquiryOptions = [
    { value: 'General Consultation Inquiry', labelEn: 'General Consultation Inquiry', labelAm: 'አጠቃላይ የምክክር ጥያቄ' },
    { value: 'Diaspora Family Care Coordination', labelEn: 'Diaspora Family Care Coordination', labelAm: 'የዲያስፖራ ቤተሰብ እንክብካቤ' },
    { value: 'Specialist Second Medical Opinion', labelEn: 'Specialist Second Medical Opinion', labelAm: 'የስፔሻሊስት ሁለተኛ አስተያየት' },
    { value: 'Medical Travel & Hospital Admission', labelEn: 'Medical Travel & Hospital Admission', labelAm: 'የህክምና ጉዞና ሆስፒታል ምዝገባ' },
    { value: 'Medical Equipment & Home Supplies', labelEn: 'Medical Equipment & Home Supplies', labelAm: 'የህክምና መሣሪያዎች አቅርቦት' },
  ];

  getDashboardLink(): string {
    const roles = this.auth.roles();
    if (roles.includes('Admin')) return '/admin';
    if (roles.includes('Agent')) return '/agent';
    return '/patient';
  }

  submitContact() {
    if (!this.fullName().trim() || !this.email().trim() || !this.message().trim()) {
      this.feedback.error(this.i18n.isAmharic() ? 'እባክዎ ሙሉ ስምዎን፣ ኢሜይልዎን እና መልዕክትዎን ያስገቡ።' : 'Please provide your name, email, and message.');
      return;
    }

    this.submitting.set(true);

    setTimeout(() => {
      this.submitting.set(false);
      this.submitted.set(true);
      const successText = this.i18n.isAmharic()
        ? 'መልዕክትዎ በተሳካ ሁኔታ ደርሶናል! የህክምና አስተባባሪ ቡድናችን በአጭር ጊዜ ውስጥ ያነጋግርዎታል።'
        : 'Thank you! Your message has been received. Our clinical coordination desk will contact you promptly.';
      this.feedback.success(successText);
      this.message.set('');
    }, 800);
  }
}
