import { PagedResult } from './api-response.model';

export interface UserSearchResult {
  id: string;
  fullName: string;
  userName: string;
  profilePictureUrl?: string;
  mutualFriendsCount: number;
}

export interface PostSearchResult {
  id: string;
  content?: string;
  authorName: string;
  authorAvatar?: string;
  createdAt: string;
}

export interface GroupSearchResult {
  chatId: string;
  name: string;
  description?: string;
  groupPhotoUrl?: string;
  memberCount: number;
}

export interface GlobalSearchResult {
  users: PagedResult<UserSearchResult>;
  posts: PagedResult<PostSearchResult>;
  groups: PagedResult<GroupSearchResult>;
  totalResults: number;
}
