import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminService } from '../../../core/services/admin.service';
import { DashboardStats } from '../../../core/models/admin.model';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatProgressSpinnerModule],
  template: `
    <div class="max-w-6xl mx-auto px-4 py-6">
      <h1 class="text-2xl font-bold text-gray-800 mb-6">Admin Dashboard</h1>

      <div class="flex gap-2 mb-6">
        <a routerLink="/admin/users" class="px-4 py-2 rounded-lg bg-[#1877f2] text-white text-sm font-medium no-underline">Manage Users</a>
        <a routerLink="/admin/posts" class="px-4 py-2 rounded-lg bg-white border border-gray-200 text-gray-700 text-sm font-medium no-underline">Manage Posts</a>
      </div>

      @if (loading()) {
        <div class="flex justify-center py-16"><mat-spinner diameter="40"></mat-spinner></div>
      }

      @if (error()) {
        <div class="bg-red-50 text-red-600 p-4 rounded-xl">{{ error() }}</div>
      }

      @if (stats(); as s) {
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          @for (card of cards(s); track card.label) {
            <div class="bg-white rounded-xl shadow-sm p-5 flex items-center gap-4">
              <div class="w-12 h-12 rounded-full flex items-center justify-center" [class]="card.bg">
                <mat-icon [class]="card.fg">{{ card.icon }}</mat-icon>
              </div>
              <div>
                <p class="text-2xl font-bold text-gray-800">{{ card.value }}</p>
                <p class="text-sm text-gray-500">{{ card.label }}</p>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class AdminDashboardComponent implements OnInit {
  private adminSvc = inject(AdminService);
  stats = signal<DashboardStats | null>(null);
  loading = signal(true);
  error = signal('');

  ngOnInit(): void {
    this.adminSvc.getDashboard().subscribe({
      next: res => {
        if (res.success) this.stats.set(res.data);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.message || 'Failed to load dashboard. Admin access required.');
        this.loading.set(false);
      }
    });
  }

  cards(s: DashboardStats) {
    return [
      { label: 'Total Users', value: s.totalUsers, icon: 'group', bg: 'bg-blue-100', fg: 'text-blue-600' },
      { label: 'Active Users', value: s.activeUsers, icon: 'how_to_reg', bg: 'bg-green-100', fg: 'text-green-600' },
      { label: 'Suspended', value: s.suspendedUsers, icon: 'block', bg: 'bg-red-100', fg: 'text-red-600' },
      { label: 'Total Posts', value: s.totalPosts, icon: 'article', bg: 'bg-purple-100', fg: 'text-purple-600' },
      { label: 'Reports', value: s.totalReports, icon: 'flag', bg: 'bg-orange-100', fg: 'text-orange-600' },
      { label: 'New Users Today', value: s.newUsersToday, icon: 'person_add', bg: 'bg-cyan-100', fg: 'text-cyan-600' },
      { label: 'New Posts Today', value: s.newPostsToday, icon: 'post_add', bg: 'bg-indigo-100', fg: 'text-indigo-600' },
    ];
  }
}
