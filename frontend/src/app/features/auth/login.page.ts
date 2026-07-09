import { Component, ChangeDetectionStrategy, OnInit, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/notifications/notification.service';

declare const google: {
  accounts: {
    id: {
      initialize(config: { client_id: string; callback: (r: { credential: string }) => void }): void;
      renderButton(el: HTMLElement, config: object): void;
    };
  };
};

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen flex items-center justify-center bg-[var(--color-bg)]">
      <div class="bg-[var(--color-surface)] rounded-2xl shadow-lg p-10 max-w-sm w-full text-center">
        <h1 class="text-2xl font-bold text-primary-600 mb-2">Family OS</h1>
        <p class="text-sm text-[var(--color-text-muted)] mb-6">{{ 'login.subtitle' | translate }}</p>
        <div id="google-signin-btn" data-testid="login-google-btn" class="flex justify-center"></div>
        <p class="text-xs text-[var(--color-text-muted)] mt-6">{{ 'login.privacy' | translate }}</p>
      </div>
    </div>
  `,
})
export class LoginPage implements OnInit {
  private auth = inject(AuthService);
  private notify = inject(NotificationService);
  private http = inject(HttpClient);
  private platformId = inject(PLATFORM_ID);

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.loadConfig();
  }

  private async loadConfig(): Promise<void> {
    try {
      const config = await firstValueFrom(
        this.http.get<{ googleClientId: string }>('/api/v1/auth/config')
      );
      if (!config.googleClientId) {
        this.notify.error('Google Client ID nincs beállítva. Ellenőrizd a GOOGLE_CLIENT_ID env változót.');
        return;
      }
      this.loadGoogleScript(config.googleClientId);
    } catch {
      this.notify.error('Nem sikerült betölteni a bejelentkezési konfigurációt.');
    }
  }

  private loadGoogleScript(clientId: string): void {
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => this.initGoogleSignIn(clientId);
    document.head.appendChild(script);
  }

  private initGoogleSignIn(clientId: string): void {
    google.accounts.id.initialize({
      client_id: clientId,
      callback: async (response) => {
        try {
          await this.auth.loginWithGoogleToken(response.credential);
        } catch {
          this.notify.error('Bejelentkezés sikertelen. Kérjük, próbálj újra.');
        }
      },
    });

    const btn = document.getElementById('google-signin-btn');
    if (btn) {
      google.accounts.id.renderButton(btn, {
        theme: 'outline',
        size: 'large',
        text: 'signin_with',
        locale: 'hu',
      });
    }
  }
}
