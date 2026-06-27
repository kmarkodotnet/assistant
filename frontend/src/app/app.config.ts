import { ApplicationConfig, provideZoneChangeDetection, importProvidersFrom } from '@angular/core';
import { provideRouter, withEnabledBlockingInitialNavigation, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { APP_ROUTES } from './app.routes';
import { traceIdInterceptor } from './core/api/trace-id.interceptor';
import { withCredentialsInterceptor } from './core/api/with-credentials.interceptor';
import { httpErrorInterceptor } from './core/api/http-error.interceptor';

export function createTranslateLoader(http: HttpClient) {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(
      APP_ROUTES,
      withEnabledBlockingInitialNavigation(),
      withComponentInputBinding()
    ),
    provideHttpClient(
      withInterceptors([traceIdInterceptor, withCredentialsInterceptor, httpErrorInterceptor])
    ),
    importProvidersFrom(
      TranslateModule.forRoot({
        defaultLanguage: 'hu',
        loader: {
          provide: TranslateLoader,
          useFactory: createTranslateLoader,
          deps: [HttpClient],
        },
      })
    ),
  ],
};
