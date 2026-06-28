import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { NotificationService } from '../notifications/notification.service';

export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const notify = inject(NotificationService);

  const requiredRoles = (route.data as { roles?: string[] })['roles'] ?? [];
  const role = auth.currentUser()?.role;

  if (role && requiredRoles.includes(role)) {
    return true;
  }

  notify.error('Nincs jogosultságod ehhez az oldalhoz.');
  return router.createUrlTree(['/']);
};

export const adminGuard: CanActivateFn = (_route, _state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const notify = inject(NotificationService);

  if (auth.currentUser()?.role === 'Admin') {
    return true;
  }

  notify.error('Nincs jogosultságod ehhez az oldalhoz.');
  return router.createUrlTree(['/']);
};
