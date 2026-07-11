import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./notifications.page').then(m => m.NotificationsPage),
    title: 'Értesítések — Family OS',
  },
] satisfies Routes;
