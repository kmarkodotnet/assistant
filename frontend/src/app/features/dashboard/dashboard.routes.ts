import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./dashboard.page').then(m => m.DashboardPage),
    title: 'Irányítópult — Family OS',
  },
] satisfies Routes;
