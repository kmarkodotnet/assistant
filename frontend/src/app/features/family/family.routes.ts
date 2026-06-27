import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./family.page').then(m => m.FamilyPage),
    title: 'Família — Family OS',
  },
] satisfies Routes;
