import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { authStore, currentUser, isAuthenticated, isAdmin } from './auth.store';
import type { CurrentUserDto } from './current-user.dto';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  readonly currentUser = currentUser;
  readonly isAuthenticated = isAuthenticated;
  readonly isAdmin = isAdmin;
  readonly status = authStore.select(s => s.status);

  async loadCurrentUser(): Promise<void> {
    try {
      const user = await firstValueFrom(
        this.http.get<CurrentUserDto>('/api/v1/auth/me')
      );
      authStore.update({ user, status: 'authenticated' });
    } catch {
      authStore.update({ user: null, status: 'anonymous' });
    }
  }

  async loginWithGoogleToken(idToken: string): Promise<void> {
    const user = await firstValueFrom(
      this.http.post<CurrentUserDto>('/api/v1/auth/login/google', { idToken })
    );
    authStore.update({ user, status: 'authenticated' });
    const returnUrl = new URLSearchParams(window.location.search).get('returnUrl') ?? '/';
    await this.router.navigateByUrl(returnUrl);
  }

  async logout(): Promise<void> {
    await firstValueFrom(this.http.post<void>('/api/v1/auth/logout', {}));
    authStore.set({ user: null, status: 'anonymous' });
    await this.router.navigate(['/login']);
  }
}
