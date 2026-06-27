import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./pages/family-list.page').then(m => m.FamilyListPage),
    title: 'Família — Family OS',
  },
] satisfies Routes;
