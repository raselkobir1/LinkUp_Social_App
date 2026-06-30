import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../../core/services/auth.service';
import { ResetPasswordDto } from '../../../core/models/auth.model';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-[#f0f2f5]">
      <div class="bg-white rounded-2xl shadow-lg p-8 w-full max-w-md">
        <h1 class="text-2xl font-bold text-[#1877f2] mb-1">LinkUp</h1>
        <h2 class="text-xl font-semibold text-gray-800 mb-2">Choose a new password</h2>
        <p class="text-gray-500 text-sm mb-6">Enter and confirm your new password below.</p>

        @if (done) {
          <div class="p-4 bg-green-50 border border-green-200 rounded-lg text-green-700 mb-4">
            Your password has been reset. You can now sign in.
          </div>
          <a routerLink="/auth/login" class="text-[#1877f2] text-sm hover:underline">Back to sign in</a>
        } @else {
          @if (error) {
            <div class="p-3 bg-red-50 border border-red-200 rounded-lg text-red-600 text-sm mb-4">{{ error }}</div>
          }
          <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-4">
            <mat-form-field class="w-full" appearance="outline">
              <mat-label>Email address</mat-label>
              <input matInput type="email" formControlName="email">
            </mat-form-field>
            <mat-form-field class="w-full" appearance="outline">
              <mat-label>New password</mat-label>
              <input matInput type="password" formControlName="newPassword">
            </mat-form-field>
            <mat-form-field class="w-full" appearance="outline">
              <mat-label>Confirm new password</mat-label>
              <input matInput type="password" formControlName="confirmPassword">
            </mat-form-field>
            <button mat-flat-button color="primary" type="submit" class="w-full" [disabled]="loading || form.invalid">
              {{ loading ? 'Resetting...' : 'Reset password' }}
            </button>
          </form>
          <div class="mt-4 text-center">
            <a routerLink="/auth/login" class="text-[#1877f2] text-sm hover:underline">Back to sign in</a>
          </div>
        }
      </div>
    </div>
  `
})
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]]
  });
  loading = false;
  done = false;
  error = '';
  private token = '';

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    this.token = qp.get('token') ?? '';
    const email = qp.get('email');
    if (email) this.form.patchValue({ email });
  }

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    if (v.newPassword !== v.confirmPassword) { this.error = 'Passwords do not match.'; return; }
    if (!this.token) { this.error = 'Reset token missing. Please use the link from your email.'; return; }
    this.loading = true;
    this.error = '';
    const dto: ResetPasswordDto = {
      email: v.email!, token: this.token, newPassword: v.newPassword!, confirmPassword: v.confirmPassword!
    };
    this.auth.resetPassword(dto).subscribe({
      next: () => { this.loading = false; this.done = true; },
      error: err => { this.loading = false; this.error = err?.error?.message ?? 'Failed to reset password.'; }
    });
  }
}
