import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./deadlines.page').then(m => m.DeadlinesPage),
    title: 'Határidők — Family OS',
  },
] satisfies Routes;
