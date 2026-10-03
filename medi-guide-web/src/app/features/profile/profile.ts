import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { UserService } from '../../core/services/user';
import { PatientService } from '../../core/services/patient';
import { AuthService } from '../../core/services/auth';
import { TranslationService } from '../../core/services/translation';
import { FeedbackService } from '../../core/services/feedback.service';
import { UserProfile } from '../../core/models/user.model';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    DatePipe,
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);
  private readonly patientService = inject(PatientService);
  private readonly auth = inject(AuthService);
  private readonly feedback = inject(FeedbackService);
  readonly i18n = inject(TranslationService);

  loading = signal(true);
  saving = signal(false);
  successMsg = signal<string | null>(null);
  errorMsg = signal<string | null>(null);

  profile = signal<UserProfile | null>(null);
  isPatient = computed(() => this.profile()?.roles?.includes('Patient') ?? false);
  isAgent = computed(() => this.profile()?.roles?.includes('Agent') ?? false);
  userInitial = computed(() => {
    const name = this.profile()?.fullName?.trim();
    return name && name.length > 0 ? name.charAt(0).toUpperCase() : 'U';
  });

  // Account, Clinical & Professional Practice Form
  form = this.fb.group({
    fullName: ['', [Validators.required, Validators.maxLength(150)]],
    email: [{ value: '', disabled: true }],
    phoneNumber: ['', [Validators.required, Validators.maxLength(32)]],
    preferredLanguage: ['en'],
    // Clinical intake fields (for Patients)
    dateOfBirth: [''],
    gender: [''],
    emergencyContactName: [''],
    emergencyContactPhone: [''],
    allergies: [''],
    chronicConditions: [''],
    currentMedications: [''],
    // Professional practice fields (for Agents)
    title: [''],
    department: ['General Practice'],
    specialty: [''],
    isAvailable: [true],
  });

  // Security Form (Password change)
  securityForm = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]],
  });

  changingPassword = signal(false);
  securitySuccessMsg = signal<string | null>(null);
  securityErrorMsg = signal<string | null>(null);
  hideCurrentPassword = signal(true);
  hideNewPassword = signal(true);

  ngOnInit() {
    this.loadProfile();
  }

  loadProfile() {
    this.loading.set(true);
    this.userService.getMe().subscribe({
      next: (user) => {
        this.profile.set(user);
        this.form.patchValue({
          fullName: user.fullName,
          email: user.email,
          phoneNumber: user.phoneNumber || '',
        });

        // If user is a patient, load clinical intake profile
        if (user.roles?.includes('Patient')) {
          this.patientService.getMe().subscribe({
            next: (patient) => {
              this.form.patchValue({
                preferredLanguage: patient.preferredLanguage || 'en',
                dateOfBirth: patient.dateOfBirth ? patient.dateOfBirth.substring(0, 10) : '',
                gender: patient.gender || '',
                emergencyContactName: patient.emergencyContactName || '',
                emergencyContactPhone: patient.emergencyContactPhone || '',
                allergies: patient.allergies || '',
                chronicConditions: patient.chronicConditions || '',
                currentMedications: patient.currentMedications || '',
              });
              this.loading.set(false);
            },
            error: () => this.loading.set(false),
          });
        } else if (user.roles?.includes('Agent')) {
          this.form.patchValue({
            title: user.title || '',
            department: user.department || 'General Practice',
            specialty: user.specialty || '',
            isAvailable: user.isAvailable ?? true,
          });
          this.loading.set(false);
        } else {
          this.loading.set(false);
        }
      },
      error: () => {
        this.errorMsg.set(
          this.i18n.isAmharic()
            ? 'የመገለጫ መረጃ ማግኘት አልተቻለም።'
            : 'Could not load profile.'
        );
        this.loading.set(false);
      },
    });
  }

  saveProfile() {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.successMsg.set(null);
    this.errorMsg.set(null);

    const val = this.form.getRawValue();

    // 1. Update general user info & agent details
    this.userService
      .updateMe({
        fullName: val.fullName!,
        phoneNumber: val.phoneNumber || undefined,
        title: this.isAgent() ? (val.title || undefined) : undefined,
        department: this.isAgent() ? (val.department || undefined) : undefined,
        specialty: this.isAgent() ? (val.specialty || undefined) : undefined,
        isAvailable: this.isAgent() ? (val.isAvailable ?? true) : undefined,
      })
      .subscribe({
        next: (updatedUser) => {
          this.profile.set(updatedUser);

          // 2. If patient, update clinical profile
          if (this.isPatient()) {
            this.patientService
              .updateMe({
                fullName: val.fullName!,
                phoneNumber: val.phoneNumber || undefined,
                preferredLanguage: val.preferredLanguage || undefined,
                dateOfBirth: val.dateOfBirth || null,
                gender: val.gender || null,
                emergencyContactName: val.emergencyContactName || null,
                emergencyContactPhone: val.emergencyContactPhone || null,
                allergies: val.allergies || null,
                chronicConditions: val.chronicConditions || null,
                currentMedications: val.currentMedications || null,
              })
              .subscribe({
                next: () => {
                  this.saving.set(false);
                  const msg = this.i18n.t('profile.saved');
                  this.successMsg.set(msg);
                  this.feedback.success(msg);
                },
                error: () => {
                  this.saving.set(false);
                  const err = this.i18n.isAmharic()
                    ? 'የህክምና መረጃን ማስተካከል አልተቻለም።'
                    : 'Failed to update clinical profile.';
                  this.errorMsg.set(err);
                  this.feedback.error(err);
                },
              });
          } else {
            this.saving.set(false);
            const msg = this.i18n.t('profile.saved');
            this.successMsg.set(msg);
            this.feedback.success(msg);
          }
        },
        error: () => {
          this.saving.set(false);
          const err = this.i18n.isAmharic()
            ? 'መረጃውን ማስተካከል አልተቻለም።'
            : 'Failed to update user profile.';
          this.errorMsg.set(err);
          this.feedback.error(err);
        },
      });
  }

  updatePassword() {
    if (this.securityForm.invalid) return;

    const val = this.securityForm.getRawValue();
    if (val.newPassword !== val.confirmPassword) {
      const mismatch = this.i18n.t('profile.passMismatch');
      this.securityErrorMsg.set(mismatch);
      this.feedback.error(mismatch);
      return;
    }

    this.changingPassword.set(true);
    this.securitySuccessMsg.set(null);
    this.securityErrorMsg.set(null);

    this.userService
      .updateMe({
        fullName: this.profile()?.fullName || '',
        currentPassword: val.currentPassword!,
        newPassword: val.newPassword!,
      })
      .subscribe({
        next: () => {
          this.changingPassword.set(false);
          const passMsg = this.i18n.isAmharic()
            ? 'የይለፍ ቃልዎ በተሳካ ሁኔታ ተቀይሯል!'
            : 'Password changed successfully!';
          this.securitySuccessMsg.set(passMsg);
          this.feedback.success(passMsg);
          this.securityForm.reset();
        },
        error: (err) => {
          this.changingPassword.set(false);
          const msg =
            err.error?.[0] ||
            (this.i18n.isAmharic()
              ? 'የይለፍ ቃል መቀየር አልተቻለም። እባክዎ የአሁኑን የይለፍ ቃል ያረጋግጡ።'
              : 'Failed to change password. Please verify your current password.');
          this.securityErrorMsg.set(msg);
          this.feedback.error(msg);
        },
      });
  }
}
