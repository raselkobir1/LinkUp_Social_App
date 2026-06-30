import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink, Router } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { SearchService } from '../../core/services/search.service';
import { FriendService } from '../../core/services/friend.service';
import { ChatService } from '../../core/services/chat.service';
import { UserSearchResult, PostSearchResult, GroupSearchResult } from '../../core/models/search.model';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, RouterLink, MatTabsModule, MatProgressSpinnerModule, MatButtonModule, MatIconModule],
  template: `
    <div class="max-w-4xl mx-auto px-4 py-6">
      @if (query()) {
        <h2 class="text-xl font-bold text-gray-800 mb-4">Search results for "{{ query() }}"</h2>
      }

      @if (loading()) {
        <div class="flex justify-center py-10"><mat-spinner diameter="40"></mat-spinner></div>
      } @else if (query()) {
        <mat-tab-group>
          <!-- People -->
          <mat-tab [label]="'People (' + users().length + ')'">
            <div class="mt-4 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              @for (user of users(); track user.id) {
                <div class="bg-white rounded-xl shadow-sm p-4 flex items-center gap-3">
                  <a [routerLink]="['/profile', user.id]">
                    <img [src]="user.profilePictureUrl || 'assets/default-avatar.png'" class="w-12 h-12 rounded-full object-cover">
                  </a>
                  <div class="flex-1 min-w-0">
                    <a [routerLink]="['/profile', user.id]" class="font-semibold text-gray-800 hover:underline truncate block">{{ user.fullName }}</a>
                    <p class="text-sm text-gray-400 truncate">{{ '@' + user.userName }}</p>
                    @if (user.mutualFriendsCount > 0) { <p class="text-xs text-gray-400">{{ user.mutualFriendsCount }} mutual</p> }
                  </div>
                  <button mat-stroked-button color="primary" (click)="sendRequest(user.id)"><mat-icon>person_add</mat-icon></button>
                </div>
              }
              @if (users().length === 0) { <p class="text-gray-400 col-span-full text-center py-8">No people found</p> }
            </div>
          </mat-tab>

          <!-- Posts -->
          <mat-tab [label]="'Posts (' + posts().length + ')'">
            <div class="mt-4 max-w-2xl mx-auto space-y-3">
              @for (post of posts(); track post.id) {
                <a [routerLink]="['/profile']" class="block bg-white rounded-xl shadow-sm p-4 no-underline">
                  <div class="flex items-center gap-2 mb-2">
                    <img [src]="post.authorAvatar || 'assets/default-avatar.png'" class="w-8 h-8 rounded-full object-cover">
                    <span class="text-sm font-medium text-gray-800">{{ post.authorName }}</span>
                  </div>
                  <p class="text-sm text-gray-700">{{ post.content }}</p>
                </a>
              }
              @if (posts().length === 0) { <p class="text-gray-400 text-center py-8">No posts found</p> }
            </div>
          </mat-tab>

          <!-- Groups -->
          <mat-tab [label]="'Groups (' + groups().length + ')'">
            <div class="mt-4 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              @for (group of groups(); track group.chatId) {
                <button (click)="openGroup(group.chatId)" class="bg-white rounded-xl shadow-sm p-4 flex items-center gap-3 text-left">
                  <img [src]="group.groupPhotoUrl || 'assets/default-avatar.png'" class="w-12 h-12 rounded-full object-cover">
                  <div class="flex-1 min-w-0">
                    <p class="font-semibold text-gray-800 truncate">{{ group.name }}</p>
                    <p class="text-sm text-gray-400">{{ group.memberCount }} members</p>
                  </div>
                </button>
              }
              @if (groups().length === 0) { <p class="text-gray-400 col-span-full text-center py-8">No groups found</p> }
            </div>
          </mat-tab>
        </mat-tab-group>

        @if (users().length === 0 && posts().length === 0 && groups().length === 0) {
          <div class="text-center py-16 text-gray-400">
            <mat-icon class="text-5xl text-gray-200">search_off</mat-icon>
            <p class="mt-2">No results found for "{{ query() }}"</p>
          </div>
        }
      }
    </div>
  `
})
export class SearchComponent implements OnInit {
  private searchSvc = inject(SearchService);
  private friendSvc = inject(FriendService);
  private chatSvc = inject(ChatService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  query = signal('');
  users = signal<UserSearchResult[]>([]);
  posts = signal<PostSearchResult[]>([]);
  groups = signal<GroupSearchResult[]>([]);
  loading = signal(false);

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['q']) {
        this.query.set(params['q']);
        this.search(params['q']);
      }
    });
  }

  search(q: string): void {
    this.loading.set(true);
    this.searchSvc.globalSearch(q).subscribe({
      next: res => {
        if (res.success) {
          this.users.set(res.data.users?.items ?? []);
          this.posts.set(res.data.posts?.items ?? []);
          this.groups.set(res.data.groups?.items ?? []);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  sendRequest(userId: string): void {
    this.friendSvc.sendRequest(userId).subscribe();
  }

  openGroup(chatId: string): void {
    this.router.navigate(['/messages', chatId]);
  }
}
