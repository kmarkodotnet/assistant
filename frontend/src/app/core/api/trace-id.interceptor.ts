import { HttpInterceptorFn } from '@angular/common/http';

export const traceIdInterceptor: HttpInterceptorFn = (req, next) => {
  const traceId = crypto.randomUUID();
  const cloned = req.clone({
    setHeaders: { traceparent: `00-${traceId.replace(/-/g, '')}-0000000000000001-01` },
  });
  return next(cloned);
};
