import { Routes } from '@angular/router';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./shared/shell/shell').then((m) => m.Shell),
    children: [
      {
        path: 'patient',
        loadComponent: () =>
          import('./features/patient/dashboard/dashboard').then((m) => m.Dashboard),
        canActivate: [roleGuard('Patient')],
      },
      {
        path: 'patient/bookings/:id',
        loadComponent: () =>
          import('./features/patient/booking-detail/booking-detail').then(
            (m) => m.BookingDetail
          ),
        canActivate: [roleGuard('Patient')],
      },
      {
        path: 'agent',
        loadComponent: () =>
          import('./features/agent/dashboard/dashboard').then((m) => m.Dashboard),
        canActivate: [roleGuard('Agent')],
      },
      {
        path: 'agent/bookings/:id',
        loadComponent: () =>
          import('./features/patient/booking-detail/booking-detail').then(
            (m) => m.BookingDetail
          ),
        canActivate: [roleGuard('Agent')],
      },
      {
        path: 'admin',
        loadComponent: () =>
          import('./features/admin/dashboard/dashboard').then((m) => m.Dashboard),
        canActivate: [roleGuard('Admin')],
      },
      {
        path: 'admin/bookings/:id',
        loadComponent: () =>
          import('./features/patient/booking-detail/booking-detail').then(
            (m) => m.BookingDetail
          ),
        canActivate: [roleGuard('Admin')],
      },
    ],
  },
  { path: 'login', loadComponent: () => import('./features/auth/login/login').then((m) => m.Login) },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
];