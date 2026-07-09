import { Routes } from '@angular/router';
import { adminGuard } from '../../core/auth/role.guard';

export default [
  {
    path: '',
    canActivate: [adminGuard],
    loadComponent: () => import('./admin.page').then(m => m.AdminPage),
    children: [
      {
        path: '',
        redirectTo: 'audit',
        pathMatch: 'full',
      },
      {
        path: 'audit',
        loadComponent: () => import('./pages/audit-log.page').then(m => m.AuditLogPage),
        title: 'Audit napló — Family OS',
      },
      {
        path: 'security-events',
        loadComponent: () => import('./pages/security-events.page').then(m => m.SecurityEventsPage),
        title: 'Biztonsági események — Family OS',
      },
      {
        path: 'jobs',
        loadComponent: () => import('./pages/ai-jobs.page').then(m => m.AiJobsPage),
        title: 'AI feladatok — Family OS',
      },
      {
        path: 'providers',
        loadComponent: () => import('./pages/ai-providers.page').then(m => m.AiProvidersPage),
        title: 'AI providerek — Family OS',
      },
    ],
  },
] satisfies Routes;
