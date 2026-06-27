import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./suggestions.page').then(m => m.SuggestionsPage),
    title: 'Javaslatok — Family OS',
  },
] satisfies Routes;
