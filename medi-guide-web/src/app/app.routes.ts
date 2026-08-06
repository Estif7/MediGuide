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
  { path: 'agent', loadComponent: () => import('./features/agent/dashboard/dashboard').then(m => m.Dashboard) },
  { path: 'admin', loadComponent: () => import('./features/admin/dashboard/dashboard').then(m => m.Dashboard) },
];