export interface CommentDto {
  id: string;
  postId: string;
  authorId: string;
  authorName: string;
  authorProfilePicture?: string;
  content: string;
  likeCount: number;
  replyCount: number;
  isLiked: boolean;
  parentCommentId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCommentDto {
  postId: string;
  content: string;
  parentCommentId?: string;
}

export interface UpdateCommentDto { content: string; }
