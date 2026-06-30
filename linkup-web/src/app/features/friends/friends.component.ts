import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';
import { FriendService } from '../../core/services/friend.service';
import { FriendDto, FriendRequestDto, BlockedUserDto } from '../../core/models/friend.model';

@Component({
  selector: 'app-friends',
  standalone: true,
  imports: [CommonModule, RouterLink, MatTabsModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatMenuModule],
  templateUrl: './friends.component.html',
  styleUrls: ['./friends.component.scss']
})
export class FriendsComponent implements OnInit {
  private friendSvc = inject(FriendService);

  pendingRequests = signal<FriendRequestDto[]>([]);
  sentRequests = signal<FriendRequestDto[]>([]);
  friends = signal<FriendDto[]>([]);
  suggestions = signal<FriendDto[]>([]);
  blocked = signal<BlockedUserDto[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.friendSvc.getPendingRequests().subscribe(res => {
      if (res.success) this.pendingRequests.set(res.data.items);
    });
    this.friendSvc.getSentRequests().subscribe(res => {
      if (res.success) this.sentRequests.set(res.data.items);
    });
    this.friendSvc.getFriends().subscribe(res => {
      if (res.success) this.friends.set(res.data.items);
      this.loading.set(false);
    });
    this.friendSvc.getSuggestions().subscribe(res => {
      if (res.success) this.suggestions.set(res.data);
    });
    this.friendSvc.getBlocked().subscribe(res => {
      if (res.success) this.blocked.set(res.data);
    });
  }

  acceptRequest(requestId: string, senderId: string): void {
    this.friendSvc.acceptRequest(requestId).subscribe({
      next: () => {
        this.pendingRequests.update(reqs => reqs.filter(r => r.id !== requestId));
      }
    });
  }

  rejectRequest(requestId: string): void {
    this.friendSvc.rejectRequest(requestId).subscribe({
      next: () => this.pendingRequests.update(reqs => reqs.filter(r => r.id !== requestId))
    });
  }

  sendRequest(userId: string): void {
    this.friendSvc.sendRequest(userId).subscribe({
      next: () => this.suggestions.update(s => s.filter(u => u.userId !== userId))
    });
  }

  unfriend(userId: string): void {
    if (!confirm('Remove this friend?')) return;
    this.friendSvc.unfriend(userId).subscribe({
      next: () => this.friends.update(f => f.filter(x => x.userId !== userId))
    });
  }

  cancelRequest(requestId: string): void {
    this.friendSvc.cancelRequest(requestId).subscribe({
      next: () => this.sentRequests.update(reqs => reqs.filter(r => r.id !== requestId))
    });
  }

  blockUser(userId: string, fullName: string, profilePictureUrl?: string): void {
    if (!confirm('Block this user? You will no longer be friends or see each other.')) return;
    this.friendSvc.blockUser(userId).subscribe({
      next: () => {
        this.friends.update(f => f.filter(x => x.userId !== userId));
        this.blocked.update(b => [...b, { id: userId, fullName, profilePictureUrl }]);
      }
    });
  }

  unblockUser(userId: string): void {
    this.friendSvc.unblockUser(userId).subscribe({
      next: () => this.blocked.update(b => b.filter(x => x.id !== userId))
    });
  }
}
