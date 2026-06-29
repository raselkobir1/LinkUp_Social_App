import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminService } from '../../../core/services/admin.service';
import { AdminUser } from '../../../core/models/admin.model';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <div class="max-w-6xl mx-auto px-4 py-6">
      <div class="flex items-center gap-3 mb-6">
        <a routerLink="/admin/dashboard" class="text-gray-500 hover:text-gray-800"><mat-icon>arrow_back</mat-icon></a>
        <h1 class="text-2xl font-bold text-gray-800">Manage Users</h1>
      </div>

      <div class="flex items-center bg-white rounded-full px-4 py-2 shadow-sm mb-4 max-w-md">
        <mat-icon class="text-gray-400 mr-2">search</mat-icon>
        <input type="text" [(ngModel)]="search" (keyup.enter)="load()"
          placeholder="Search users by name or email..."
          class="bg-transparent outline-none text-sm w-full">
      </div>

      @if (loading()) {
        <div class="flex justify-center py-16"><mat-spinner diameter="40"></mat-spinner></div>
      }

      @if (error()) {
        <div class="bg-red-50 text-red-600 p-4 rounded-xl">{{ error() }}</div>
      }

      <div class="space-y-2">
        @for (user of users(); track user.id) {
          <div class="bg-white rounded-xl shadow-sm p-4 flex items-center gap-4">
            <img [src]="user.profilePictureUrl || 'assets/default-avatar.png'" class="w-12 h-12 rounded-full object-cover">
            <div class="flex-1 min-w-0">
              <p class="font-semibold text-gray-800 truncate">
                {{ user.fullName }}
                @if (user.roles.includes('Admin')) {
                  <span class="ml-2 text-xs bg-purple-100 text-purple-600 px-2 py-0.5 rounded-full">Admin</span>
                }
                @if (user.isSuspended) {
                  <span class="ml-2 text-xs bg-red-100 text-red-600 px-2 py-0.5 rounded-full">Suspended</span>
                }
              </p>
              <p class="text-sm text-gray-400 truncate">{{ user.email }} &middot; {{ '@' + user.userName }}</p>
            </div>
            @if (user.isSuspended) {
              <button mat-stroked-button color="primary" (click)="unsuspend(user)">Unsuspend</button>
            } @else {
              <button mat-stroked-button color="warn" (click)="suspend(user)">Suspend</button>
            }
            <button mat-icon-button color="warn" (click)="remove(user)" title="Deactivate"><mat-icon>delete</mat-icon></button>
          </div>
        }

        @if (!loading() && users().length === 0) {
          <div class="text-center py-16 text-gray-400">No users found</div>
        }
      </div>
    </div>
  `
})
export class AdminUsersComponent implements OnInit {
  private adminSvc = inject(AdminService);
  users = signal<AdminUser[]>([]);
  loading = signal(true);
  error = signal('');
  search = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.adminSvc.getUsers({ search: this.search, pageSize: 50 }).subscribe({
      next: res => {
        if (res.success) this.users.set(res.data.items);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.message || 'Failed to load users. Admin access required.');
        this.loading.set(false);
      }
    });
  }

  suspend(user: AdminUser): void {
    const reason = prompt(`Reason for suspending ${user.fullName}?`, 'Violation of community guidelines');
    if (reason == null) return;
    this.adminSvc.suspendUser(user.id, reason).subscribe({
      next: () => this.users.update(list => list.map(u => u.id === user.id ? { ...u, isSuspended: true, suspensionReason: reason } : u))
    });
  }

  unsuspend(user: AdminUser): void {
    this.adminSvc.unsuspendUser(user.id).subscribe({
      next: () => this.users.update(list => list.map(u => u.id === user.id ? { ...u, isSuspended: false } : u))
    });
  }

  remove(user: AdminUser): void {
    if (!confirm(`Deactivate ${user.fullName}? This cannot be easily undone.`)) return;
    this.adminSvc.deleteUser(user.id).subscribe({
      next: () => this.users.update(list => list.filter(u => u.id !== user.id))
    });
  }
}
