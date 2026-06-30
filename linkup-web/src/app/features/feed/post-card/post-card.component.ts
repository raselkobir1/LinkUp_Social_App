import { Component, Input, Output, EventEmitter, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PostDto, ReactionType, PostVisibility } from '../../../core/models/post.model';
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
  @Output() postShared = new EventEmitter<PostDto>();

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

  // Edit post
  editing = signal(false);
  editContent = '';
  editVisibility: PostVisibility = 'Public';
  // Share
  showShareBox = signal(false);
  shareContent = '';
  sharing = signal(false);
  // Per-comment interaction state
  editingCommentId = signal<string | null>(null);
  editCommentContent = '';
  replyingToId = signal<string | null>(null);
  replyContent = '';
  repliesByComment = signal<Record<string, CommentDto[]>>({});

  isCommentOwner(comment: CommentDto): boolean {
    return comment.authorId === this.auth.currentUser()?.id;
  }

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

  startEdit(): void {
    this.editContent = this.post.content;
    this.editVisibility = this.post.visibility;
    this.editing.set(true);
  }

  cancelEdit(): void {
    this.editing.set(false);
  }

  saveEdit(): void {
    const content = this.editContent.trim();
    this.postSvc.update(this.post.id, { content, visibility: this.editVisibility }).subscribe({
      next: res => {
        if (res.success) {
          this.post = { ...this.post, content, visibility: this.editVisibility };
          this.editing.set(false);
          this.postUpdated.emit(this.post);
        }
      }
    });
  }

  toggleShareBox(): void {
    this.showShareBox.update(v => !v);
  }

  reportPost(): void {
    const reason = prompt('Why are you reporting this post?');
    if (!reason?.trim()) return;
    this.postSvc.report(this.post.id, reason.trim()).subscribe({
      next: res => { if (res.success) alert('Thanks — this post has been reported.'); }
    });
  }

  sharePost(): void {
    this.sharing.set(true);
    this.postSvc.share({ originalPostId: this.post.id, content: this.shareContent.trim(), visibility: 'Public' }).subscribe({
      next: res => {
        this.sharing.set(false);
        if (res.success) {
          this.post = { ...this.post, shareCount: this.post.shareCount + 1 };
          this.shareContent = '';
          this.showShareBox.set(false);
          this.postShared.emit(res.data);
        }
      },
      error: () => this.sharing.set(false)
    });
  }

  likeComment(comment: CommentDto): void {
    const op = comment.isLiked ? this.commentSvc.unlikeComment(comment.id) : this.commentSvc.likeComment(comment.id);
    const delta = comment.isLiked ? -1 : 1;
    op.subscribe({
      next: () => this.patchComment(comment.id, c => ({ ...c, isLiked: !c.isLiked, likeCount: Math.max(0, c.likeCount + delta) }))
    });
  }

  startEditComment(comment: CommentDto): void {
    this.editingCommentId.set(comment.id);
    this.editCommentContent = comment.content;
  }

  cancelEditComment(): void {
    this.editingCommentId.set(null);
  }

  saveEditComment(comment: CommentDto): void {
    const content = this.editCommentContent.trim();
    if (!content) return;
    this.commentSvc.updateComment(comment.id, { content }).subscribe({
      next: res => { if (res.success) { this.patchComment(comment.id, c => ({ ...c, content })); this.editingCommentId.set(null); } }
    });
  }

  deleteComment(comment: CommentDto): void {
    if (!confirm('Delete this comment?')) return;
    this.commentSvc.deleteComment(comment.id).subscribe({
      next: () => {
        if (comment.parentCommentId) {
          this.repliesByComment.update(map => ({
            ...map,
            [comment.parentCommentId!]: (map[comment.parentCommentId!] ?? []).filter(r => r.id !== comment.id)
          }));
        } else {
          this.comments.update(cs => cs.filter(c => c.id !== comment.id));
        }
        this.post = { ...this.post, commentCount: Math.max(0, this.post.commentCount - 1) };
      }
    });
  }

  toggleReply(comment: CommentDto): void {
    this.replyContent = '';
    this.replyingToId.update(id => id === comment.id ? null : comment.id);
    if (this.replyingToId() === comment.id && !this.repliesByComment()[comment.id]) this.loadReplies(comment);
  }

  loadReplies(comment: CommentDto): void {
    this.commentSvc.getReplies(comment.id).subscribe({
      next: res => { if (res.success) this.repliesByComment.update(map => ({ ...map, [comment.id]: res.data.items })); }
    });
  }

  submitReply(comment: CommentDto): void {
    const content = this.replyContent.trim();
    if (!content) return;
    this.commentSvc.addComment({ postId: this.post.id, content, parentCommentId: comment.id }).subscribe({
      next: res => {
        if (res.success) {
          this.repliesByComment.update(map => ({ ...map, [comment.id]: [...(map[comment.id] ?? []), res.data] }));
          this.patchComment(comment.id, c => ({ ...c, replyCount: c.replyCount + 1 }));
          this.post = { ...this.post, commentCount: this.post.commentCount + 1 };
          this.replyContent = '';
        }
      }
    });
  }

  private patchComment(id: string, fn: (c: CommentDto) => CommentDto): void {
    this.comments.update(cs => cs.map(c => c.id === id ? fn(c) : c));
    this.repliesByComment.update(map => {
      const next: Record<string, CommentDto[]> = {};
      for (const key of Object.keys(map)) next[key] = map[key].map(c => c.id === id ? fn(c) : c);
      return next;
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
