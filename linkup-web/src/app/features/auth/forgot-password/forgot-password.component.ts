import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-[#f0f2f5]">
      <div class="bg-white rounded-2xl shadow-lg p-8 w-full max-w-md">
        <h1 class="text-2xl font-bold text-[#1877f2] mb-1">LinkUp</h1>
        <h2 class="text-xl font-semibold text-gray-800 mb-2">Reset your password</h2>
        <p class="text-gray-500 text-sm mb-6">Enter your email and we'll send you a reset link.</p>

        @if (sent) {
          <div class="p-4 bg-green-50 border border-green-200 rounded-lg text-green-700 mb-4">
            Check your email for a password reset link.
          </div>
        }
        @if (error) {
          <div class="p-3 bg-red-50 border border-red-200 rounded-lg text-red-600 text-sm mb-4">{{ error }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-4">
          <mat-form-field class="w-full" appearance="outline">
            <mat-label>Email address</mat-label>
            <input matInput type="email" formControlName="email">
          </mat-form-field>
          <button mat-flat-button color="primary" type="submit" class="w-full" [disabled]="loading || form.invalid">
            {{ loading ? 'Sending...' : 'Send reset link' }}
          </button>
        </form>
        <div class="mt-4 text-center">
          <a routerLink="/auth/login" class="text-[#1877f2] text-sm hover:underline">Back to sign in</a>
        </div>
      </div>
    </div>
  `
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);

  form = this.fb.group({ email: ['', [Validators.required, Validators.email]] });
  loading = false;
  sent = false;
  error = '';

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.auth.forgotPassword(this.form.value as { email: string }).subscribe({
      next: () => { this.loading = false; this.sent = true; },
      error: err => { this.loading = false; this.error = err?.error?.message ?? 'Failed to send reset link.'; }
    });
  }
}
