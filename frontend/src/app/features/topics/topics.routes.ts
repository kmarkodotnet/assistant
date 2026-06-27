import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./topics.page').then(m => m.TopicsPage),
    title: 'Témák — Family OS',
  },
] satisfies Routes;
