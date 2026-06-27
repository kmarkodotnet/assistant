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

      if (err.status === 401) {
        void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      } else if (err.status === 403) {
        notify.error(appError.message || 'Nincs jogosultságod ehhez a művelethez.');
      }

      return throwError(() => appError);
    })
  );
};
