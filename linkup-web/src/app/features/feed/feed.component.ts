import { Component, inject, OnInit, OnDestroy, AfterViewInit, ViewChild, ElementRef, NgZone, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PostService } from '../../core/services/post.service';
import { PostDto } from '../../core/models/post.model';
import { PostCardComponent } from './post-card/post-card.component';
import { CreatePostComponent } from './create-post/create-post.component';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-feed',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule, PostCardComponent, CreatePostComponent],
  templateUrl: './feed.component.html',
  styleUrls: ['./feed.component.scss']
})
export class FeedComponent implements OnInit, AfterViewInit, OnDestroy {
  private postSvc = inject(PostService);
  private zone = inject(NgZone);
  auth = inject(AuthService);

  posts = signal<PostDto[]>([]);
  loading = signal(false);
  loadingMore = signal(false);
  page = 1;
  hasMore = true;

  // Sentinel observed by IntersectionObserver to auto-load the next page.
  @ViewChild('sentinel') private sentinel?: ElementRef<HTMLElement>;
  private observer?: IntersectionObserver;

  ngOnInit(): void { this.loadFeed(); }

  ngAfterViewInit(): void {
    this.observer = new IntersectionObserver(entries => {
      // Run inside Angular's zone so loadMore's state changes are picked up.
      if (entries.some(e => e.isIntersecting)) this.zone.run(() => this.loadMore());
    }, { rootMargin: '400px' });
    if (this.sentinel) this.observer.observe(this.sentinel.nativeElement);
  }

  ngOnDestroy(): void { this.observer?.disconnect(); }

  loadFeed(): void {
    this.loading.set(true);
    this.postSvc.getFeed(1, 10).subscribe({
      next: res => {
        if (res.success) {
          this.posts.set(res.data.items);
          this.hasMore = res.data.hasNextPage;
          this.page = 1;
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  loadMore(): void {
    if (this.loadingMore() || this.loading() || !this.hasMore) return;
    this.loadingMore.set(true);
    this.page++;
    this.postSvc.getFeed(this.page, 10).subscribe({
      next: res => {
        if (res.success) {
          this.posts.update(p => [...p, ...res.data.items]);
          this.hasMore = res.data.hasNextPage;
        }
        this.loadingMore.set(false);
      },
      error: () => this.loadingMore.set(false)
    });
  }

  onPostCreated(post: PostDto): void {
    this.posts.update(p => [post, ...p]);
  }

  onPostDeleted(id: string): void {
    this.posts.update(p => p.filter(x => x.id !== id));
  }

  onPostUpdated(post: PostDto): void {
    this.posts.update(p => p.map(x => x.id === post.id ? post : x));
  }
}
