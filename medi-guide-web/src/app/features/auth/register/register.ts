import { Component, signal, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth';
import { TranslationService } from '../../../core/services/translation';
import { ThemeService } from '../../../core/services/theme';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatIconModule,
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly i18n = inject(TranslationService);
  readonly themeService = inject(ThemeService);

  currentStep = signal<1 | 2>(1);
  error = signal<string | null>(null);
  loading = signal(false);
  hidePassword = signal(true);

  accountForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    phoneNumber: ['', [Validators.required, Validators.maxLength(32)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    preferredLanguage: ['en'],
  });

  clinicalForm = this.fb.nonNullable.group({
    dateOfBirth: [''],
    gender: [''],
    emergencyContactName: [''],
    emergencyContactPhone: [''],
    allergies: [''],
    chronicConditions: [''],
    currentMedications: [''],
  });

  goToStep2() {
    if (this.accountForm.invalid) {
      this.accountForm.markAllAsTouched();
      return;
    }
    this.currentStep.set(2);
  }

  goToStep1() {
    this.currentStep.set(1);
  }

  submit() {
    if (this.accountForm.invalid) {
      this.currentStep.set(1);
      this.accountForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const accountVal = this.accountForm.getRawValue();
    const clinicalVal = this.clinicalForm.getRawValue();

    const payload = {
      fullName: accountVal.fullName,
      email: accountVal.email,
      phoneNumber: accountVal.phoneNumber,
      password: accountVal.password,
      preferredLanguage: accountVal.preferredLanguage,
      dateOfBirth: clinicalVal.dateOfBirth ? new Date(clinicalVal.dateOfBirth).toISOString() : undefined,
      gender: clinicalVal.gender || undefined,
      emergencyContactName: clinicalVal.emergencyContactName || undefined,
      emergencyContactPhone: clinicalVal.emergencyContactPhone || undefined,
      allergies: clinicalVal.allergies || undefined,
      chronicConditions: clinicalVal.chronicConditions || undefined,
      currentMedications: clinicalVal.currentMedications || undefined,
    };

    this.auth.registerPatient(payload).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigateByUrl('/patient');
      },
      error: (err) => {
        this.loading.set(false);
        let msg = 'Registration failed. Please check your inputs.';
        if (Array.isArray(err.error)) {
          msg = err.error.join(' ');
        } else if (typeof err.error === 'string') {
          msg = err.error;
        } else if (err.error?.errors) {
          const validationMsgs = Object.values(err.error.errors).flat();
          msg = validationMsgs.join(' ');
        } else if (err.error?.title) {
          msg = err.error.title;
        } else if (err.message) {
          msg = err.message;
        }
        this.error.set(msg);
      },
    });
  }
}
