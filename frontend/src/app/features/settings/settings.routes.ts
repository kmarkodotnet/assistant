import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./settings.page').then(m => m.SettingsPage),
    title: 'Beállítások — Family OS',
  },
] satisfies Routes;
