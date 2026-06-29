import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { SearchService } from '../../core/services/search.service';
import { FriendService } from '../../core/services/friend.service';

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
      }

      <!-- Users -->
      @if (users().length > 0) {
        <h3 class="font-semibold text-gray-700 mb-3">People</h3>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mb-8">
          @for (user of users(); track $index) {
            <div class="bg-white rounded-xl shadow-sm p-4 flex items-center gap-3">
              <a [routerLink]="['/profile', user.id]">
                <img [src]="user.profilePictureUrl || 'assets/default-avatar.png'"
                  class="w-12 h-12 rounded-full object-cover">
              </a>
              <div class="flex-1 min-w-0">
                <a [routerLink]="['/profile', user.id]" class="font-semibold text-gray-800 hover:underline truncate block">
                  {{ user.fullName }}
                </a>
                <p class="text-sm text-gray-400 truncate">{{ '@' + user.userName }}</p>
              </div>
              <button mat-stroked-button color="primary" (click)="sendRequest(user.id)">
                <mat-icon>person_add</mat-icon>
              </button>
            </div>
          }
        </div>
      }

      @if (!loading() && users().length === 0 && query()) {
        <div class="text-center py-16 text-gray-400">
          <mat-icon class="text-5xl text-gray-200">search_off</mat-icon>
          <p class="mt-2">No results found for "{{ query() }}"</p>
        </div>
      }
    </div>
  `
})
export class SearchComponent implements OnInit {
  private searchSvc = inject(SearchService);
  private friendSvc = inject(FriendService);
  private route = inject(ActivatedRoute);

  query = signal('');
  users = signal<any[]>([]);
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
    this.searchSvc.searchUsers(q).subscribe({
      next: (res: any) => {
        if (res.success) this.users.set(res.data.items ?? res.data ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  sendRequest(userId: string): void {
    this.friendSvc.sendRequest(userId).subscribe();
  }
}
