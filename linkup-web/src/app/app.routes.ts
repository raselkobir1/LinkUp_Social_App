import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/feed', pathMatch: 'full' },
  {
    path: 'auth',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.authRoutes)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./features/layout/shell/shell.component').then(m => m.ShellComponent),
    children: [
      { path: 'feed', loadComponent: () => import('./features/feed/feed.component').then(m => m.FeedComponent) },
      { path: 'profile/:userId', loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent) },
      { path: 'friends', loadComponent: () => import('./features/friends/friends.component').then(m => m.FriendsComponent) },
      { path: 'messages', loadComponent: () => import('./features/chat/chat.component').then(m => m.ChatComponent) },
      { path: 'messages/:chatId', loadComponent: () => import('./features/chat/chat.component').then(m => m.ChatComponent) },
      { path: 'notifications', loadComponent: () => import('./features/notifications/notifications.component').then(m => m.NotificationsComponent) },
      { path: 'search', loadComponent: () => import('./features/search/search.component').then(m => m.SearchComponent) },
      { path: 'settings', loadComponent: () => import('./features/settings/settings.component').then(m => m.SettingsComponent) },
      { path: 'admin', loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes) },
      { path: 'video-call/:callId', loadComponent: () => import('./features/video-call/video-call.component').then(m => m.VideoCallComponent) },
      { path: 'calls', loadComponent: () => import('./features/video-call/call-history.component').then(m => m.CallHistoryComponent) },
    ]
  },
  { path: '**', redirectTo: '/feed' }
];
