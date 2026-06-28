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
        title: 'Személyes beállítások — Family OS',
      },
      {
        path: 'system',
        loadComponent: () => import('./pages/settings-system.page').then(m => m.SettingsSystemPage),
        title: 'Rendszer — Family OS',
      },
      {
        path: 'integrations',
        loadComponent: () => import('./pages/integrations.page').then(m => m.IntegrationsPage),
        title: 'Integrációk — Family OS',
      },
      {
        path: 'ai-providers',
        loadComponent: () => import('./pages/ai-providers-settings.page').then(m => m.AiProvidersSettingsPage),
        title: 'AI providerek — Family OS',
      },
      {
        path: 'backup',
        loadComponent: () => import('./pages/backup.page').then(m => m.BackupPage),
        title: 'Biztonsági mentések — Family OS',
      },
    ],
  },
] satisfies Routes;
