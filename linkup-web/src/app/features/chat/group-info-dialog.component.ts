import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ChatService } from '../../core/services/chat.service';
import { FriendService } from '../../core/services/friend.service';
import { MediaService } from '../../core/services/media.service';
import { AuthService } from '../../core/services/auth.service';
import { GroupChatDto } from '../../core/models/chat.model';
import { FriendDto } from '../../core/models/friend.model';

@Component({
  selector: 'app-group-info-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatCheckboxModule],
  template: `
    <h2 mat-dialog-title>Group info</h2>
    <mat-dialog-content class="!pt-2">
      @if (group(); as g) {
        <!-- Photo + name -->
        <div class="flex items-center gap-4 mb-4">
          <div class="relative">
            <img [src]="g.groupPhotoUrl || 'assets/default-avatar.png'" class="w-16 h-16 rounded-full object-cover">
            @if (isAdmin()) {
              <label class="absolute -bottom-1 -right-1 bg-gray-100 rounded-full p-1 cursor-pointer shadow hover:bg-gray-200">
                <mat-icon style="font-size:16px;height:16px;width:16px">photo_camera</mat-icon>
                <input type="file" accept="image/*" class="hidden" (change)="changePhoto($event)">
              </label>
            }
          </div>
          <div class="flex-1">
            @if (isAdmin()) {
              <div class="flex items-center gap-2">
                <input [(ngModel)]="editName" class="border-b border-gray-200 outline-none text-lg font-semibold flex-1">
                <button mat-icon-button (click)="saveName()" title="Save name"><mat-icon>check</mat-icon></button>
              </div>
            } @else {
              <p class="text-lg font-semibold text-gray-800">{{ g.name }}</p>
            }
            <p class="text-sm text-gray-400">{{ g.memberCount }} members</p>
          </div>
        </div>

        <!-- Members -->
        <p class="text-sm font-medium text-gray-700 mb-1">Members</p>
        <div class="space-y-1 mb-4">
          @for (m of g.members; track m.userId) {
            <div class="flex items-center gap-3 p-2 rounded-lg hover:bg-gray-50">
              <img [src]="m.profilePictureUrl || 'assets/default-avatar.png'" class="w-9 h-9 rounded-full object-cover">
              <div class="flex-1">
                <p class="text-sm text-gray-800">{{ m.fullName }}</p>
                @if (m.isAdmin) { <p class="text-xs text-[#1877f2]">Admin</p> }
              </div>
              @if (isAdmin() && m.userId !== currentUserId) {
                <button mat-icon-button title="Make admin" (click)="makeAdmin(m.userId)" [disabled]="m.isAdmin">
                  <mat-icon class="text-gray-400">shield</mat-icon>
                </button>
                <button mat-icon-button title="Remove" (click)="removeMember(m.userId)">
                  <mat-icon class="text-gray-400">person_remove</mat-icon>
                </button>
              }
            </div>
          }
        </div>

        <!-- Add members -->
        @if (isAdmin() && addableFriends().length > 0) {
          <p class="text-sm font-medium text-gray-700 mb-1">Add members</p>
          <div class="max-h-40 overflow-y-auto space-y-1 mb-2">
            @for (f of addableFriends(); track f.userId) {
              <label class="flex items-center gap-3 p-2 rounded-lg hover:bg-gray-50 cursor-pointer">
                <mat-checkbox [checked]="toAdd().has(f.userId)" (change)="toggleAdd(f.userId)"></mat-checkbox>
                <img [src]="f.profilePictureUrl || 'assets/default-avatar.png'" class="w-8 h-8 rounded-full object-cover">
                <span class="text-sm text-gray-800">{{ f.fullName }}</span>
              </label>
            }
          </div>
          <button mat-stroked-button color="primary" [disabled]="toAdd().size === 0" (click)="addMembers()">
            <mat-icon>person_add</mat-icon> Add selected
          </button>
        }
      } @else {
        <p class="text-gray-400 text-sm">Loading…</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button color="warn" (click)="leave()">Leave group</button>
      <button mat-button (click)="close()">Done</button>
    </mat-dialog-actions>
  `
})
export class GroupInfoDialogComponent implements OnInit {
  private chatSvc = inject(ChatService);
  private friendSvc = inject(FriendService);
  private mediaSvc = inject(MediaService);
  private auth = inject(AuthService);
  private dialogRef = inject(MatDialogRef<GroupInfoDialogComponent>);
  data = inject<{ chatId: string }>(MAT_DIALOG_DATA);

  group = signal<GroupChatDto | null>(null);
  friends = signal<FriendDto[]>([]);
  toAdd = signal<Set<string>>(new Set());
  editName = '';
  private changed = false;
  currentUserId = this.auth.currentUser()?.id ?? '';

  isAdmin = computed(() => this.group()?.members.find(m => m.userId === this.currentUserId)?.isAdmin ?? false);
  addableFriends = computed(() => {
    const memberIds = new Set(this.group()?.members.map(m => m.userId) ?? []);
    return this.friends().filter(f => !memberIds.has(f.userId));
  });

  ngOnInit(): void {
    this.reload();
    this.friendSvc.getFriends(1, 100).subscribe({ next: r => { if (r.success) this.friends.set(r.data.items); } });
  }

  reload(): void {
    this.chatSvc.getGroupInfo(this.data.chatId).subscribe({
      next: r => { if (r.success) { this.group.set(r.data); this.editName = r.data.name; } }
    });
  }

  saveName(): void {
    const g = this.group();
    if (!g || !this.editName.trim()) return;
    this.chatSvc.updateGroup(this.data.chatId, { name: this.editName.trim(), description: g.description }).subscribe({
      next: r => { if (r.success) { this.group.set(r.data); this.changed = true; } }
    });
  }

  changePhoto(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.mediaSvc.uploadImage(file).subscribe({
      next: up => {
        if (!up.success) return;
        this.chatSvc.changeGroupPhoto(this.data.chatId, up.data.url).subscribe({
          next: () => { this.group.update(g => g ? { ...g, groupPhotoUrl: up.data.url } : g); this.changed = true; }
        });
      }
    });
  }

  toggleAdd(userId: string): void {
    this.toAdd.update(s => { const ns = new Set(s); ns.has(userId) ? ns.delete(userId) : ns.add(userId); return ns; });
  }

  addMembers(): void {
    const ids = Array.from(this.toAdd());
    if (ids.length === 0) return;
    this.chatSvc.addGroupMembers(this.data.chatId, ids).subscribe({
      next: r => { if (r.success) { this.group.set(r.data); this.toAdd.set(new Set()); this.changed = true; } }
    });
  }

  makeAdmin(userId: string): void {
    this.chatSvc.makeGroupAdmin(this.data.chatId, userId).subscribe({
      next: () => { this.group.update(g => g ? { ...g, members: g.members.map(m => m.userId === userId ? { ...m, isAdmin: true } : m) } : g); this.changed = true; }
    });
  }

  removeMember(userId: string): void {
    this.chatSvc.removeGroupMember(this.data.chatId, userId).subscribe({
      next: () => { this.group.update(g => g ? { ...g, members: g.members.filter(m => m.userId !== userId), memberCount: g.memberCount - 1 } : g); this.changed = true; }
    });
  }

  leave(): void {
    if (!confirm('Leave this group?')) return;
    this.chatSvc.leaveGroup(this.data.chatId).subscribe({
      next: () => this.dialogRef.close({ left: true })
    });
  }

  close(): void { this.dialogRef.close({ changed: this.changed }); }
}
