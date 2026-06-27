import { Routes } from '@angular/router';

export default [
  {
    path: '',
    loadComponent: () => import('./pages/documents-list.page').then(m => m.DocumentsListPage),
    title: 'Dokumentumok — Family OS',
  },
  {
    path: 'upload',
    loadComponent: () => import('./pages/document-upload.page').then(m => m.DocumentUploadPage),
    title: 'Feltöltés — Family OS',
  },
  {
    path: ':id',
    loadComponent: () => import('./pages/document-detail.page').then(m => m.DocumentDetailPage),
    title: 'Dokumentum — Family OS',
  },
] satisfies Routes;
