import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';

export const APP_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.page').then(m => m.LoginPage),
    title: 'Bejelentkezés — Family OS',
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell.component').then(m => m.ShellComponent),
    children: [
      { path: '',           loadChildren: () => import('./features/dashboard/dashboard.routes') },
      { path: 'documents',  loadChildren: () => import('./features/documents/documents.routes') },
      { path: 'notes',      loadChildren: () => import('./features/notes/notes.routes') },
      { path: 'search',     loadChildren: () => import('./features/search/search.routes') },
      { path: 'tasks',      loadChildren: () => import('./features/tasks/tasks.routes') },
      { path: 'deadlines',  loadChildren: () => import('./features/deadlines/deadlines.routes') },
      { path: 'reminders',  loadChildren: () => import('./features/reminders/reminders.routes') },
      { path: 'topics',     loadChildren: () => import('./features/topics/topics.routes') },
      {
        path: 'family',
        loadChildren: () => import('./features/family/family.routes'),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
      },
      { path: 'suggestions', loadChildren: () => import('./features/suggestions/suggestions.routes') },
      { path: 'settings',   loadChildren: () => import('./features/settings/settings.routes') },
      {
        path: 'admin',
        loadChildren: () => import('./features/admin/admin.routes'),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
