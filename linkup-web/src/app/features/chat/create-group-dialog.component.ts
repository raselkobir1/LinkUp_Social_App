import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ChatService } from '../../core/services/chat.service';
import { FriendService } from '../../core/services/friend.service';
import { FriendDto } from '../../core/models/friend.model';

@Component({
  selector: 'app-create-group-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatCheckboxModule],
  template: `
    <h2 mat-dialog-title>New group</h2>
    <mat-dialog-content class="!pt-2">
      <mat-form-field class="w-full" appearance="outline">
        <mat-label>Group name</mat-label>
        <input matInput [(ngModel)]="name">
      </mat-form-field>
      <mat-form-field class="w-full" appearance="outline">
        <mat-label>Description (optional)</mat-label>
        <input matInput [(ngModel)]="description">
      </mat-form-field>

      <p class="text-sm font-medium text-gray-700 mb-2">Add members</p>
      <div class="max-h-60 overflow-y-auto space-y-1">
        @for (f of friends(); track f.userId) {
          <label class="flex items-center gap-3 p-2 rounded-lg hover:bg-gray-50 cursor-pointer">
            <mat-checkbox [checked]="selected().has(f.userId)" (change)="toggle(f.userId)"></mat-checkbox>
            <img [src]="f.profilePictureUrl || 'assets/default-avatar.png'" class="w-8 h-8 rounded-full object-cover">
            <span class="text-sm text-gray-800">{{ f.fullName }}</span>
          </label>
        }
        @if (friends().length === 0) {
          <p class="text-sm text-gray-400 py-4 text-center">Add some friends first to create a group.</p>
        }
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Cancel</button>
      <button mat-flat-button color="primary" (click)="create()" [disabled]="!canCreate() || saving()">
        {{ saving() ? 'Creating...' : 'Create group' }}
      </button>
    </mat-dialog-actions>
  `
})
export class CreateGroupDialogComponent implements OnInit {
  private chatSvc = inject(ChatService);
  private friendSvc = inject(FriendService);
  private dialogRef = inject(MatDialogRef<CreateGroupDialogComponent>);

  name = '';
  description = '';
  friends = signal<FriendDto[]>([]);
  selected = signal<Set<string>>(new Set());
  saving = signal(false);

  ngOnInit(): void {
    this.friendSvc.getFriends(1, 100).subscribe({ next: r => { if (r.success) this.friends.set(r.data.items); } });
  }

  toggle(userId: string): void {
    this.selected.update(s => {
      const ns = new Set(s);
      ns.has(userId) ? ns.delete(userId) : ns.add(userId);
      return ns;
    });
  }

  canCreate(): boolean {
    return this.name.trim().length > 0 && this.selected().size > 0;
  }

  create(): void {
    if (!this.canCreate()) return;
    this.saving.set(true);
    this.chatSvc.createGroup({
      name: this.name.trim(),
      description: this.description.trim() || undefined,
      memberIds: Array.from(this.selected())
    }).subscribe({
      next: res => { this.saving.set(false); if (res.success) this.dialogRef.close(res.data); },
      error: () => this.saving.set(false)
    });
  }

  close(): void { this.dialogRef.close(); }
}
