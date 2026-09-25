import { Routes } from '@angular/router';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  // Standalone public pages
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/home/landing/landing').then((m) => m.Landing),
  },
  {
    path: 'about',
    loadComponent: () => import('./features/home/about/about').then((m) => m.About),
  },
  {
    path: 'contact',
    loadComponent: () => import('./features/home/contact/contact').then((m) => m.Contact),
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
  },

  // Authenticated Portal Pages within Shell Layout
  {
    path: '',
    loadComponent: () => import('./shared/shell/shell').then((m) => m.Shell),
    children: [
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile').then((m) => m.Profile),
      },
      {
        path: 'patient',
        loadComponent: () =>
          import('./features/patient/dashboard/dashboard').then((m) => m.Dashboard),
        canActivate: [roleGuard('Patient')],
      },
      {
        path: 'patient/bookings',
        redirectTo: 'patient',
        pathMatch: 'full',
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
        path: 'agent/bookings',
        redirectTo: 'agent',
        pathMatch: 'full',
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
        path: 'admin/users',
        loadComponent: () =>
          import('./features/admin/users/users').then((m) => m.AdminUsers),
        canActivate: [roleGuard('Admin')],
      },
      {
        path: 'admin/bookings',
        redirectTo: 'admin',
        pathMatch: 'full',
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
  { path: '**', redirectTo: '' },
];