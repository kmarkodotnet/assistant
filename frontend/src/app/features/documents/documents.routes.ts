import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./documents.page').then(m => m.DocumentsPage),
    title: 'Dokumentumok — Family OS',
  },
] satisfies Routes;
