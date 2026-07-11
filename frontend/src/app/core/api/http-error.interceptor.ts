import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../notifications/notification.service';
import type { AppError } from './app-error.model';

interface ProblemDetails {
  detail?: string;
  traceId?: string;
  type?: string;
  fieldErrors?: Record<string, string[]>;
}

function isProblemDetails(val: unknown): val is ProblemDetails {
  return typeof val === 'object' && val !== null;
}

/**
 * Végpontok, amelyeknél a 401 NEM a felhasználó munkamenetének lejártát
 * jelenti, hanem egy domain-specifikus, üzleti hibát — ezekre a globális
 * interceptor nem navigálhat `/login`-ra, a hívónak kell kezelnie.
 * api-design.md §16.3.1: a `tool-calls/confirm|reject` 401-je a
 * proposal-token lejártára/érvénytelenségére vonatkozik, nem a session-re.
 */
const SESSION_401_EXCLUDED_PREFIXES = ['/api/v1/tool-calls/'];

export function isSessionAuth401(req: { url: string }): boolean {
  return !SESSION_401_EXCLUDED_PREFIXES.some(prefix => req.url.includes(prefix));
}

export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const notify = inject(NotificationService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const body = isProblemDetails(err.error) ? err.error : null;
      const appError: AppError = {
        code: body?.type ?? `http_${err.status}`,
        message: body?.detail ?? 'Ismeretlen hiba történt.',
        traceId: body?.traceId ?? '',
        fieldErrors: body?.fieldErrors ?? null,
      };

      if (err.status === 401 && isSessionAuth401(req)) {
        void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      } else if (err.status === 403) {
        notify.error(appError.message || 'Nincs jogosultságod ehhez a művelethez.');
      }

      return throwError(() => appError);
    })
  );
};
