import { HttpInterceptorFn } from '@angular/common/http';

export const withCredentialsInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.url.startsWith('/api/') || req.url.startsWith('/healthz')) {
    return next(req.clone({ withCredentials: true }));
  }
  return next(req);
};
