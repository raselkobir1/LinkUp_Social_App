import { Routes } from '@angular/router';

export const adminRoutes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', loadComponent: () => import('./dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent) },
  { path: 'users', loadComponent: () => import('./users/admin-users.component').then(m => m.AdminUsersComponent) },
  { path: 'posts', loadComponent: () => import('./posts/admin-posts.component').then(m => m.AdminPostsComponent) },
];
