import { Component, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PostService } from '../../../core/services/post.service';
import { MediaService } from '../../../core/services/media.service';
import { AuthService } from '../../../core/services/auth.service';
import { PostDto, PostVisibility } from '../../../core/models/post.model';

@Component({
  selector: 'app-create-post',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatIconModule, MatSelectModule, MatProgressSpinnerModule],
  templateUrl: './create-post.component.html',
  styleUrls: ['./create-post.component.scss']
})
export class CreatePostComponent {
  @Output() postCreated = new EventEmitter<PostDto>();

  private postSvc = inject(PostService);
  private mediaSvc = inject(MediaService);
  auth = inject(AuthService);

  content = '';
  visibility: PostVisibility = 'Friends';
  uploadedImageUrls: string[] = [];
  uploadedVideoUrl = '';
  uploadedVideoThumbnail = '';
  loading = signal(false);
  uploadingMedia = signal(false);

  submit(): void {
    if (!this.content.trim() && this.uploadedImageUrls.length === 0 && !this.uploadedVideoUrl) return;
    this.loading.set(true);

    const postType = this.uploadedVideoUrl ? 'Video'
      : this.uploadedImageUrls.length > 0 ? (this.content ? 'Mixed' : 'Image')
      : 'Text';

    this.postSvc.create({
      content: this.content,
      postType,
      visibility: this.visibility,
      imageUrls: this.uploadedImageUrls,
      videoUrl: this.uploadedVideoUrl || undefined,
      videoThumbnailUrl: this.uploadedVideoThumbnail || undefined
    }).subscribe({
      next: res => {
        this.loading.set(false);
        if (res.success) {
          this.postCreated.emit(res.data);
          this.reset();
        }
      },
      error: () => this.loading.set(false)
    });
  }

  onImageSelected(event: Event): void {
    const files = (event.target as HTMLInputElement).files;
    if (!files?.length) return;
    this.uploadingMedia.set(true);
    const uploads = Array.from(files).map(file =>
      new Promise<void>((resolve, reject) => {
        this.mediaSvc.uploadImage(file).subscribe({
          next: res => { if (res.success) this.uploadedImageUrls.push(res.data.url); resolve(); },
          error: reject
        });
      })
    );
    Promise.all(uploads).finally(() => this.uploadingMedia.set(false));
  }

  onVideoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.uploadingMedia.set(true);
    this.mediaSvc.uploadVideo(file).subscribe({
      next: res => {
        if (res.success) {
          this.uploadedVideoUrl = res.data.url;
          this.uploadedVideoThumbnail = res.data.thumbnailUrl ?? '';
        }
        this.uploadingMedia.set(false);
      },
      error: () => this.uploadingMedia.set(false)
    });
  }

  removeImage(url: string): void {
    this.uploadedImageUrls = this.uploadedImageUrls.filter(u => u !== url);
  }

  private reset(): void {
    this.content = '';
    this.uploadedImageUrls = [];
    this.uploadedVideoUrl = '';
    this.uploadedVideoThumbnail = '';
  }
}
