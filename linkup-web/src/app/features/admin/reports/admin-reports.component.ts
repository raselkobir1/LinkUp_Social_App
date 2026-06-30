import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminService } from '../../../core/services/admin.service';
import { AdminReport } from '../../../core/models/admin.model';

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <div class="max-w-4xl mx-auto px-4 py-6">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-bold text-gray-800">Reported Posts</h1>
        <a routerLink="/admin/dashboard" class="text-[#1877f2] text-sm">← Back to dashboard</a>
      </div>

      <label class="flex items-center gap-2 mb-4 text-sm text-gray-600">
        <input type="checkbox" [checked]="includeResolved()" (change)="toggleResolved()"> Show resolved
      </label>

      @if (loading()) {
        <div class="flex justify-center py-16"><mat-spinner diameter="40"></mat-spinner></div>
      } @else if (reports().length === 0) {
        <div class="text-center py-16 text-gray-400">
          <mat-icon class="text-5xl text-gray-200">flag</mat-icon>
          <p class="mt-2">No reports to review</p>
        </div>
      } @else {
        <div class="space-y-3">
          @for (r of reports(); track r.id) {
            <div class="bg-white rounded-xl shadow-sm p-4">
              <div class="flex items-start justify-between gap-3">
                <div class="flex-1 min-w-0">
                  <p class="text-sm text-gray-500">
                    Reported by <span class="font-medium text-gray-700">{{ r.reportedByName }}</span> ·
                    {{ r.createdAt | date:'MMM d, h:mm a' }}
                  </p>
                  <p class="mt-1 text-sm"><span class="font-medium text-red-600">Reason:</span> {{ r.reason }}</p>
                  <div class="mt-2 bg-gray-50 rounded-lg p-3">
                    <p class="text-xs text-gray-500 mb-1">Post by {{ r.postAuthorName }}</p>
                    <p class="text-sm text-gray-800">{{ r.postContent || '(no text content)' }}</p>
                  </div>
                  @if (r.isResolved) { <span class="inline-block mt-2 text-xs text-green-600">Resolved</span> }
                </div>
                @if (!r.isResolved) {
                  <div class="flex flex-col gap-2 flex-shrink-0">
                    <button mat-flat-button color="warn" (click)="deletePost(r)">
                      <mat-icon>delete</mat-icon> Delete post
                    </button>
                    <button mat-stroked-button (click)="resolve(r)">
                      <mat-icon>check</mat-icon> Dismiss
                    </button>
                  </div>
                }
              </div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class AdminReportsComponent implements OnInit {
  private adminSvc = inject(AdminService);

  reports = signal<AdminReport[]>([]);
  loading = signal(true);
  includeResolved = signal(false);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.adminSvc.getReports(1, 50, this.includeResolved()).subscribe({
      next: res => { if (res.success) this.reports.set(res.data.items); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  toggleResolved(): void {
    this.includeResolved.update(v => !v);
    this.load();
  }

  resolve(r: AdminReport): void {
    this.adminSvc.resolveReport(r.id).subscribe({
      next: () => this.reports.update(list => this.includeResolved()
        ? list.map(x => x.id === r.id ? { ...x, isResolved: true } : x)
        : list.filter(x => x.id !== r.id))
    });
  }

  deletePost(r: AdminReport): void {
    if (!confirm('Delete this post and resolve the report?')) return;
    this.adminSvc.deletePost(r.postId).subscribe({
      next: () => this.adminSvc.resolveReport(r.id).subscribe({
        next: () => this.reports.update(list => list.filter(x => x.id !== r.id))
      })
    });
  }
}
