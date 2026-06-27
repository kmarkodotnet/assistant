import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./notes.page').then(m => m.NotesPage),
    title: 'Jegyzetek — Family OS',
  },
] satisfies Routes;
