import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { ProfileService } from '../../core/services/profile.service';
import { ChangePasswordDto } from '../../core/models/auth.model';
import { NotificationSettingsDto } from '../../core/models/notification.model';
import { PrivacySettingsDto } from '../../core/models/profile.model';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatSlideToggleModule, MatIconModule
  ],
  template: `
    <div class="max-w-2xl mx-auto px-4 py-6 space-y-6">
      <h1 class="text-2xl font-bold text-gray-800">Settings</h1>

      <!-- Change password -->
      <section class="bg-white rounded-xl shadow-sm p-6">
        <h2 class="text-lg font-semibold text-gray-800 mb-4 flex items-center gap-2">
          <mat-icon class="text-gray-500">lock</mat-icon> Change password
        </h2>
        @if (pwMsg()) {
          <div class="p-3 rounded-lg text-sm mb-4" [class]="pwOk() ? 'bg-green-50 text-green-700 border border-green-200' : 'bg-red-50 text-red-600 border border-red-200'">
            {{ pwMsg() }}
          </div>
        }
        <form [formGroup]="pwForm" (ngSubmit)="changePassword()" class="space-y-3">
          <mat-form-field class="w-full" appearance="outline">
            <mat-label>Current password</mat-label>
            <input matInput type="password" formControlName="currentPassword">
          </mat-form-field>
          <mat-form-field class="w-full" appearance="outline">
            <mat-label>New password</mat-label>
            <input matInput type="password" formControlName="newPassword">
          </mat-form-field>
          <mat-form-field class="w-full" appearance="outline">
            <mat-label>Confirm new password</mat-label>
            <input matInput type="password" formControlName="confirmPassword">
          </mat-form-field>
          <button mat-flat-button color="primary" type="submit" [disabled]="pwForm.invalid || pwSaving()">
            {{ pwSaving() ? 'Saving...' : 'Update password' }}
          </button>
        </form>
      </section>

      <!-- Notification settings -->
      <section class="bg-white rounded-xl shadow-sm p-6">
        <h2 class="text-lg font-semibold text-gray-800 mb-4 flex items-center gap-2">
          <mat-icon class="text-gray-500">notifications</mat-icon> Notifications
        </h2>
        @if (notif()) {
          <div class="space-y-3">
            @for (item of notifToggles; track item.key) {
              <div class="flex items-center justify-between">
                <span class="text-gray-700">{{ item.label }}</span>
                <mat-slide-toggle [checked]="notif()![item.key]" (change)="setNotif(item.key, $event.checked)"></mat-slide-toggle>
              </div>
            }
          </div>
          @if (notifMsg()) { <p class="text-green-600 text-sm mt-3">{{ notifMsg() }}</p> }
        } @else {
          <p class="text-gray-400 text-sm">Loading…</p>
        }
      </section>

      <!-- Privacy settings -->
      <section class="bg-white rounded-xl shadow-sm p-6">
        <h2 class="text-lg font-semibold text-gray-800 mb-4 flex items-center gap-2">
          <mat-icon class="text-gray-500">shield</mat-icon> Privacy
        </h2>
        @if (privacyForm) {
          <form [formGroup]="privacyForm" (ngSubmit)="savePrivacy()" class="space-y-3">
            <mat-form-field class="w-full" appearance="outline">
              <mat-label>Who can see my profile</mat-label>
              <mat-select formControlName="profileVisibility">
                @for (o of visibilityOptions; track o.value) { <mat-option [value]="o.value">{{ o.label }}</mat-option> }
              </mat-select>
            </mat-form-field>
            <mat-form-field class="w-full" appearance="outline">
              <mat-label>Who can see my friend list</mat-label>
              <mat-select formControlName="friendListVisibility">
                @for (o of visibilityOptions; track o.value) { <mat-option [value]="o.value">{{ o.label }}</mat-option> }
              </mat-select>
            </mat-form-field>
            <mat-form-field class="w-full" appearance="outline">
              <mat-label>Default post visibility</mat-label>
              <mat-select formControlName="postDefaultVisibility">
                @for (o of visibilityOptions; track o.value) { <mat-option [value]="o.value">{{ o.label }}</mat-option> }
              </mat-select>
            </mat-form-field>
            <button mat-flat-button color="primary" type="submit" [disabled]="privacySaving()">
              {{ privacySaving() ? 'Saving...' : 'Save privacy settings' }}
            </button>
            @if (privacyMsg()) { <span class="text-green-600 text-sm ml-3">{{ privacyMsg() }}</span> }
          </form>
        }
      </section>
    </div>
  `
})
export class SettingsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private notifSvc = inject(NotificationService);
  private profileSvc = inject(ProfileService);

  // Change password
  pwForm = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required]
  });
  pwSaving = signal(false);
  pwMsg = signal('');
  pwOk = signal(false);

  // Notifications
  notif = signal<NotificationSettingsDto | null>(null);
  notifMsg = signal('');
  notifToggles: { key: keyof NotificationSettingsDto; label: string }[] = [
    { key: 'friendRequests', label: 'Friend requests' },
    { key: 'postReactions', label: 'Post reactions' },
    { key: 'comments', label: 'Comments' },
    { key: 'mentions', label: 'Mentions' },
    { key: 'messages', label: 'Messages' },
    { key: 'groupInvites', label: 'Group invites' },
    { key: 'videoCalls', label: 'Video calls' }
  ];

  // Privacy
  visibilityOptions = [
    { value: 'Public', label: 'Public' },
    { value: 'Friends', label: 'Friends' },
    { value: 'OnlyMe', label: 'Only me' }
  ];
  privacyForm = this.fb.group({
    profileVisibility: ['Public'],
    friendListVisibility: ['Friends'],
    postDefaultVisibility: ['Public']
  });
  privacySaving = signal(false);
  privacyMsg = signal('');

  ngOnInit(): void {
    this.notifSvc.getSettings().subscribe({ next: r => { if (r.success) this.notif.set(r.data); } });
    this.profileSvc.getPrivacySettings().subscribe({ next: r => { if (r.success && r.data) this.privacyForm.patchValue(r.data); } });
  }

  changePassword(): void {
    if (this.pwForm.invalid) return;
    const v = this.pwForm.value;
    if (v.newPassword !== v.confirmPassword) { this.pwOk.set(false); this.pwMsg.set('New passwords do not match.'); return; }
    this.pwSaving.set(true);
    this.pwMsg.set('');
    this.auth.changePassword(v as ChangePasswordDto).subscribe({
      next: () => { this.pwSaving.set(false); this.pwOk.set(true); this.pwMsg.set('Password updated successfully.'); this.pwForm.reset(); },
      error: err => { this.pwSaving.set(false); this.pwOk.set(false); this.pwMsg.set(err?.error?.message ?? 'Failed to change password.'); }
    });
  }

  setNotif(key: keyof NotificationSettingsDto, checked: boolean): void {
    const current = this.notif();
    if (!current) return;
    const updated = { ...current, [key]: checked };
    this.notif.set(updated);
    this.notifSvc.updateSettings(updated).subscribe({
      next: () => { this.notifMsg.set('Saved'); setTimeout(() => this.notifMsg.set(''), 1500); },
      error: () => { this.notif.set(current); }
    });
  }

  savePrivacy(): void {
    this.privacySaving.set(true);
    this.privacyMsg.set('');
    this.profileSvc.updatePrivacySettings(this.privacyForm.value as PrivacySettingsDto).subscribe({
      next: () => { this.privacySaving.set(false); this.privacyMsg.set('Saved'); setTimeout(() => this.privacyMsg.set(''), 1500); },
      error: () => { this.privacySaving.set(false); this.privacyMsg.set('Failed to save.'); }
    });
  }
}
