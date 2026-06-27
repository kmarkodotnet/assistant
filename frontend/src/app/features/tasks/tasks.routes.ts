import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./tasks.page').then(m => m.TasksPage),
    title: 'Feladatok — Family OS',
  },
] satisfies Routes;
