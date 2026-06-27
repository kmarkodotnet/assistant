import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./pages/settings.page').then(m => m.SettingsPage),
    children: [
      { path: '', redirectTo: 'preferences', pathMatch: 'full' },
      {
        path: 'preferences',
        loadComponent: () => import('./pages/preferences.page').then(m => m.PreferencesPage),
        title: 'Beállítások — Family OS',
      },
      {
        path: 'system',
        loadComponent: () => import('./pages/settings-system.page').then(m => m.SettingsSystemPage),
        title: 'Rendszer — Family OS',
      },
    ],
  },
] satisfies Routes;
