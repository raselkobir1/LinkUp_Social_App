export type PostVisibility = 'Public' | 'Friends' | 'OnlyMe';
export type PostType = 'Text' | 'Image' | 'Video' | 'Mixed';
export type ReactionType = 'Like' | 'Love' | 'Haha' | 'Wow' | 'Sad' | 'Angry';

export interface PostAuthorDto {
  id: string;
  fullName: string;
  userName: string;
  profilePictureUrl?: string;
}

export interface PostImageDto {
  id: string;
  url: string;
  thumbnailUrl?: string;
  displayOrder: number;
}

export interface PostVideoDto {
  id: string;
  url: string;
  thumbnailUrl?: string;
}

export interface PostDto {
  id: string;
  content: string;
  postType: PostType;
  visibility: PostVisibility;
  isPinned: boolean;
  shareCount: number;
  commentCount: number;
  reactionCount: number;
  author: PostAuthorDto;
  images: PostImageDto[];
  video?: PostVideoDto;
  originalPost?: PostDto;
  originalPostId?: string;
  wallUserId?: string;
  userReaction?: ReactionType;
  reactionCounts: Record<ReactionType, number>;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
}

export interface CreatePostDto {
  content: string;
  postType: PostType;
  visibility: PostVisibility;
  imageUrls: string[];
  videoUrl?: string;
  videoThumbnailUrl?: string;
  wallUserId?: string;
}

export interface UpdatePostDto {
  content: string;
  visibility: PostVisibility;
}

export interface SharePostDto {
  originalPostId: string;
  content: string;
  visibility: PostVisibility;
  wallUserId?: string;
}
