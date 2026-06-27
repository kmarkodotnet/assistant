import { computed } from '@angular/core';
import { createStore } from '../state/create-store';
import type { CurrentUserDto } from './current-user.dto';

export type AuthStatus = 'unknown' | 'authenticated' | 'anonymous';

export interface AuthState {
  user: CurrentUserDto | null;
  status: AuthStatus;
}

export const authStore = createStore<AuthState>({ user: null, status: 'unknown' });
export const currentUser = computed(() => authStore.state().user);
export const isAuthenticated = computed(() => authStore.state().status === 'authenticated');
export const isAdmin = computed(() => currentUser()?.role === 'Admin');
export const isAdult = computed(() => currentUser()?.role === 'Admin' || currentUser()?.role === 'Adult');
