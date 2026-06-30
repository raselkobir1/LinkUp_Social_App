import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterLink, MatProgressSpinnerModule, MatIconModule],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-[#f0f2f5]">
      <div class="bg-white rounded-2xl shadow-lg p-8 w-full max-w-md text-center">
        <h1 class="text-2xl font-bold text-[#1877f2] mb-4">LinkUp</h1>
        @switch (status()) {
          @case ('loading') {
            <mat-spinner class="mx-auto" [diameter]="40"></mat-spinner>
            <p class="text-gray-600 mt-4">Verifying your email…</p>
          }
          @case ('success') {
            <mat-icon class="text-green-500 !w-14 !h-14 !text-5xl">check_circle</mat-icon>
            <h2 class="text-xl font-semibold text-gray-800 mt-2 mb-1">Email verified</h2>
            <p class="text-gray-500 text-sm mb-6">Your email address has been confirmed.</p>
            <a routerLink="/auth/login" class="text-[#1877f2] text-sm hover:underline">Continue to sign in</a>
          }
          @case ('error') {
            <mat-icon class="text-red-500 !w-14 !h-14 !text-5xl">error</mat-icon>
            <h2 class="text-xl font-semibold text-gray-800 mt-2 mb-1">Verification failed</h2>
            <p class="text-gray-500 text-sm mb-6">{{ error() }}</p>
            <a routerLink="/auth/login" class="text-[#1877f2] text-sm hover:underline">Back to sign in</a>
          }
        }
      </div>
    </div>
  `
})
export class VerifyEmailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);

  status = signal<'loading' | 'success' | 'error'>('loading');
  error = signal('');

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    const userId = qp.get('userId');
    const token = qp.get('token');
    if (!userId || !token) {
      this.status.set('error');
      this.error.set('This verification link is invalid or incomplete.');
      return;
    }
    this.auth.verifyEmail(userId, token).subscribe({
      next: () => this.status.set('success'),
      error: err => {
        this.status.set('error');
        this.error.set(err?.error?.message ?? 'We could not verify your email.');
      }
    });
  }
}
