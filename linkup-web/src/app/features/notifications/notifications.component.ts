import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationHubService } from '../../core/signalr/notification-hub.service';
import { NotificationDto } from '../../core/models/notification.model';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <div class="max-w-2xl mx-auto px-4 py-6">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-bold text-gray-800">Notifications</h1>
        @if (notifications().some(n => !n.isRead)) {
          <button mat-stroked-button (click)="markAllRead()">Mark all as read</button>
        }
      </div>

      @if (loading()) {
        <div class="flex justify-center py-10"><mat-spinner diameter="40"></mat-spinner></div>
      }

      <div class="space-y-1">
        @for (notif of notifications(); track notif.id) {
          <div (click)="markRead(notif)"
            class="flex items-start gap-3 p-4 rounded-xl cursor-pointer transition-colors hover:bg-gray-50"
            [class.bg-blue-50]="!notif.isRead">
            <img [src]="notif.senderProfilePicture || 'assets/default-avatar.png'"
              class="w-12 h-12 rounded-full object-cover flex-shrink-0">
            <div class="flex-1">
              <p class="text-sm text-gray-800" [innerHTML]="notif.message"></p>
              <p class="text-xs text-[#1877f2] mt-1">{{ timeAgo(notif.createdAt) }}</p>
            </div>
            @if (!notif.isRead) {
              <span class="w-2.5 h-2.5 bg-[#1877f2] rounded-full flex-shrink-0 mt-1"></span>
            }
          </div>
        }

        @if (!loading() && notifications().length === 0) {
          <div class="text-center py-16 text-gray-400">No notifications yet</div>
        }
      </div>
    </div>
  `
})
export class NotificationsComponent implements OnInit {
  private notifSvc = inject(NotificationService);
  private notifHub = inject(NotificationHubService);

  notifications = signal<NotificationDto[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.loadNotifications();
    this.notifHub.notification$.subscribe(n => {
      this.notifications.update(list => [n, ...list]);
    });
  }

  loadNotifications(): void {
    this.notifSvc.getNotifications().subscribe({
      next: res => {
        if (res.success) this.notifications.set(res.data.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  markRead(notif: NotificationDto): void {
    if (notif.isRead) return;
    this.notifSvc.markAsRead(notif.id).subscribe({
      next: () => {
        this.notifications.update(list => list.map(n => n.id === notif.id ? { ...n, isRead: true } : n));
        this.notifSvc.unreadCount.update(c => Math.max(0, c - 1));
      }
    });
  }

  markAllRead(): void {
    this.notifSvc.markAllAsRead().subscribe({
      next: () => {
        this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
        this.notifSvc.unreadCount.set(0);
      }
    });
  }

  timeAgo(dateStr: string): string {
    const seconds = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
    if (seconds < 60) return 'just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    return new Date(dateStr).toLocaleDateString();
  }
}
