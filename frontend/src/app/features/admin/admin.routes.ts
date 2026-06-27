import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./admin.page').then(m => m.AdminPage),
    title: 'Admin — Family OS',
  },
] satisfies Routes;
