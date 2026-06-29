import { Component, Input, Output, EventEmitter, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PostDto, ReactionType } from '../../../core/models/post.model';
import { CommentDto } from '../../../core/models/comment.model';
import { PostService } from '../../../core/services/post.service';
import { ReactionService } from '../../../core/services/reaction.service';
import { CommentService } from '../../../core/services/comment.service';
import { AuthService } from '../../../core/services/auth.service';

const REACTION_EMOJIS: Record<ReactionType, string> = {
  Like: '👍', Love: '❤️', Haha: '😂', Wow: '😮', Sad: '😢', Angry: '😡'
};

@Component({
  selector: 'app-post-card',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, MatIconModule, MatButtonModule, MatMenuModule, MatProgressSpinnerModule],
  templateUrl: './post-card.component.html',
  styleUrls: ['./post-card.component.scss']
})
export class PostCardComponent {
  @Input({ required: true }) post!: PostDto;
  @Output() postDeleted = new EventEmitter<string>();
  @Output() postUpdated = new EventEmitter<PostDto>();

  private postSvc = inject(PostService);
  private reactionSvc = inject(ReactionService);
  private commentSvc = inject(CommentService);
  auth = inject(AuthService);

  readonly REACTION_EMOJIS = REACTION_EMOJIS;
  readonly reactionTypes: ReactionType[] = ['Like', 'Love', 'Haha', 'Wow', 'Sad', 'Angry'];

  showComments = signal(false);
  comments = signal<CommentDto[]>([]);
  newComment = '';
  loadingComments = signal(false);
  showReactionPicker = signal(false);
  isOwner = computed(() => this.post.author.id === this.auth.currentUser()?.id);

  get totalReactions(): number {
    return this.post.reactionCount;
  }

  get dominantReactionEmoji(): string {
    if (!this.post.reactionCounts) return '👍';
    const topEntry = Object.entries(this.post.reactionCounts).sort(([, a], [, b]) => b - a)[0];
    return topEntry ? REACTION_EMOJIS[topEntry[0] as ReactionType] : '👍';
  }

  toggleComments(): void {
    this.showComments.update(v => !v);
    if (this.showComments() && this.comments().length === 0) this.loadComments();
  }

  loadComments(): void {
    this.loadingComments.set(true);
    this.commentSvc.getPostComments(this.post.id).subscribe({
      next: res => {
        if (res.success) this.comments.set(res.data.items);
        this.loadingComments.set(false);
      },
      error: () => this.loadingComments.set(false)
    });
  }

  react(type: ReactionType): void {
    this.showReactionPicker.set(false);
    if (this.post.userReaction === type) {
      this.reactionSvc.removeReaction(this.post.id, 'Post').subscribe({
        next: () => {
          this.post = { ...this.post, userReaction: undefined, reactionCount: Math.max(0, this.post.reactionCount - 1) };
        }
      });
    } else {
      const wasReacted = !!this.post.userReaction;
      this.reactionSvc.react(this.post.id, 'Post', type).subscribe({
        next: () => {
          this.post = {
            ...this.post,
            userReaction: type,
            reactionCount: wasReacted ? this.post.reactionCount : this.post.reactionCount + 1
          };
        }
      });
    }
  }

  submitComment(): void {
    if (!this.newComment.trim()) return;
    this.commentSvc.addComment({ postId: this.post.id, content: this.newComment }).subscribe({
      next: res => {
        if (res.success) {
          this.comments.update(c => [res.data, ...c]);
          this.post = { ...this.post, commentCount: this.post.commentCount + 1 };
          this.newComment = '';
        }
      }
    });
  }

  deletePost(): void {
    if (!confirm('Delete this post?')) return;
    this.postSvc.delete(this.post.id).subscribe({
      next: res => { if (res.success) this.postDeleted.emit(this.post.id); }
    });
  }

  pinPost(): void {
    this.postSvc.pin(this.post.id, !this.post.isPinned).subscribe({
      next: () => { this.post = { ...this.post, isPinned: !this.post.isPinned }; }
    });
  }

  timeAgo(dateStr: string): string {
    const seconds = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
    if (seconds < 60) return 'just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    if (seconds < 604800) return `${Math.floor(seconds / 86400)}d ago`;
    return new Date(dateStr).toLocaleDateString();
  }
}
