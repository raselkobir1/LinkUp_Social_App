import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminService } from '../../../core/services/admin.service';
import { AdminPost } from '../../../core/models/admin.model';

@Component({
  selector: 'app-admin-posts',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <div class="max-w-5xl mx-auto px-4 py-6">
      <div class="flex items-center gap-3 mb-6">
        <a routerLink="/admin/dashboard" class="text-gray-500 hover:text-gray-800"><mat-icon>arrow_back</mat-icon></a>
        <h1 class="text-2xl font-bold text-gray-800">Manage Posts</h1>
      </div>

      @if (loading()) {
        <div class="flex justify-center py-16"><mat-spinner diameter="40"></mat-spinner></div>
      }

      @if (error()) {
        <div class="bg-red-50 text-red-600 p-4 rounded-xl">{{ error() }}</div>
      }

      <div class="space-y-2">
        @for (post of posts(); track post.id) {
          <div class="bg-white rounded-xl shadow-sm p-4 flex items-start gap-4"
            [class.opacity-50]="post.isDeleted">
            <div class="flex-1 min-w-0">
              <p class="font-semibold text-gray-800">{{ post.authorName }}</p>
              <p class="text-sm text-gray-600 mt-1 line-clamp-2">{{ post.content || '(no text)' }}</p>
              <p class="text-xs text-gray-400 mt-2">
                {{ post.reactionCount }} reactions &middot; {{ post.commentCount }} comments &middot; {{ post.createdAt | date:'medium' }}
                @if (post.isDeleted) { <span class="text-red-500 ml-1">&middot; deleted</span> }
              </p>
            </div>
            @if (!post.isDeleted) {
              <button mat-icon-button color="warn" (click)="remove(post)" title="Delete post"><mat-icon>delete</mat-icon></button>
            }
          </div>
        }

        @if (!loading() && posts().length === 0) {
          <div class="text-center py-16 text-gray-400">No posts found</div>
        }
      </div>
    </div>
  `
})
export class AdminPostsComponent implements OnInit {
  private adminSvc = inject(AdminService);
  posts = signal<AdminPost[]>([]);
  loading = signal(true);
  error = signal('');

  ngOnInit(): void {
    this.adminSvc.getPosts(1, 50).subscribe({
      next: res => {
        if (res.success) this.posts.set(res.data.items);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.message || 'Failed to load posts. Admin access required.');
        this.loading.set(false);
      }
    });
  }

  remove(post: AdminPost): void {
    if (!confirm('Delete this post?')) return;
    this.adminSvc.deletePost(post.id).subscribe({
      next: () => this.posts.update(list => list.map(p => p.id === post.id ? { ...p, isDeleted: true } : p))
    });
  }
}
