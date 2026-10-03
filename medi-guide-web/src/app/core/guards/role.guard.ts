import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth';

type Role = 'Patient' | 'Agent' | 'Admin';

export function roleGuard(...allowedRoles: Role[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isLoggedIn()) {
      return router.createUrlTree(['/login']);
    }

    const roleChecks: Record<Role, () => boolean> = {
      Patient: auth.isPatient,
      Agent: auth.isAgent,
      Admin: auth.isAdmin,
    };

    const allowed = allowedRoles.some((role) => roleChecks[role]());
    if (allowed) return true;

    // Logged in but wrong role — send them to their own home instead of a dead end
    if (auth.isAdmin()) return router.createUrlTree(['/admin']);
    if (auth.isAgent()) return router.createUrlTree(['/agent']);
    return router.createUrlTree(['/patient']);
  };
}