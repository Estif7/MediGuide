import { Component, signal, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  error = signal<string | null>(null);
  loading = signal(false);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  submit() {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.roles.includes('Admin')) {
          this.router.navigateByUrl('/admin');
        } else if (res.roles.includes('Agent')) {
          this.router.navigateByUrl('/agent');
        } else {
          this.router.navigateByUrl('/patient');
        }
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Invalid email or password');
      },
    });
  }
}