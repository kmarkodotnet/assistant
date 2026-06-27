import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./search.page').then(m => m.SearchPage),
    title: 'Keresés — Family OS',
  },
] satisfies Routes;
