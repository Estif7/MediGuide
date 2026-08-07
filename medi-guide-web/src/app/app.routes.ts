import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login').then((m) => m.Login),
  },

  {
    path: 'patient',
    loadComponent: () =>
      import('./features/patient/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'patient/bookings/:id',
    loadComponent: () =>
      import('./features/patient/booking-detail/booking-detail').then(
        (m) => m.BookingDetail
      ),
  },
  {
    path: 'agent',
    loadComponent: () =>
      import('./features/agent/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'agent/bookings/:id',
    loadComponent: () =>
      import('./features/patient/booking-detail/booking-detail').then(
        (m) => m.BookingDetail
      ),
  },
  {
    path: 'admin',
    loadComponent: () =>
      import('./features/admin/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'admin/bookings/:id',
    loadComponent: () =>
      import('./features/patient/booking-detail/booking-detail').then(
        (m) => m.BookingDetail
      ),
  },
];