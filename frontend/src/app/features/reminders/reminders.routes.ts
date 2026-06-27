import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./reminders.page').then(m => m.RemindersPage),
    title: 'Emlékeztetők — Family OS',
  },
] satisfies Routes;
