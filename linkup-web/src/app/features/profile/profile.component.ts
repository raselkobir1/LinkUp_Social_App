import { Component, inject, OnInit, signal, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { ProfileService } from '../../core/services/profile.service';
import { FriendService } from '../../core/services/friend.service';
import { PostService } from '../../core/services/post.service';
import { AuthService } from '../../core/services/auth.service';
import { CallService } from '../../core/services/call.service';
import { UserProfileDto } from '../../core/models/profile.model';
import { PostDto } from '../../core/models/post.model';
import { PostCardComponent } from '../feed/post-card/post-card.component';
import { CreatePostComponent } from '../feed/create-post/create-post.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule, RouterLink, MatButtonModule, MatIconModule,
    MatTabsModule, MatProgressSpinnerModule, MatDialogModule,
    PostCardComponent, CreatePostComponent
  ],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  @Input() userId!: string;

  private profileSvc = inject(ProfileService);
  private friendSvc = inject(FriendService);
  private postSvc = inject(PostService);
  private callSvc = inject(CallService);
  auth = inject(AuthService);

  profile = signal<UserProfileDto | null>(null);
  posts = signal<PostDto[]>([]);
  loading = signal(true);

  get isOwnProfile(): boolean {
    return this.userId === this.auth.currentUser()?.id;
  }

  ngOnInit(): void {
    this.loadProfile();
    this.loadWallPosts();
  }

  loadProfile(): void {
    this.profileSvc.getProfile(this.userId).subscribe({
      next: res => {
        if (res.success) this.profile.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  loadWallPosts(): void {
    this.postSvc.getWallPosts(this.userId).subscribe({
      next: res => { if (res.success) this.posts.set(res.data.items); }
    });
  }

  sendFriendRequest(): void {
    this.friendSvc.sendRequest(this.userId).subscribe({
      next: res => {
        if (res.success) this.profile.update(p => p ? { ...p, friendshipStatus: 'Pending' } : p);
      }
    });
  }

  acceptRequest(): void {
    const profile = this.profile();
    if (!profile) return;
    // Find the request ID — handled by profile data in a real app
    this.profile.update(p => p ? { ...p, friendshipStatus: 'Friends' } : p);
  }

  unfriend(): void {
    this.friendSvc.unfriend(this.userId).subscribe({
      next: () => this.profile.update(p => p ? { ...p, friendshipStatus: 'None' } : p)
    });
  }

  uploadProfilePicture(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.profileSvc.uploadProfilePicture(file).subscribe({
      next: res => {
        if (res.success) this.profile.update(p => p ? { ...p, profilePictureUrl: res.data.url } : p);
      }
    });
  }

  startVideoCall(): void {
    this.callSvc.initiateCall(this.userId, 'video');
  }

  onPostCreated(post: PostDto): void {
    this.posts.update(p => [post, ...p]);
  }

  onPostDeleted(id: string): void {
    this.posts.update(p => p.filter(x => x.id !== id));
  }
}
