export interface DashboardStats {
  totalUsers: number;
  activeUsers: number;
  suspendedUsers: number;
  totalPosts: number;
  totalReports: number;
  newUsersToday: number;
  newPostsToday: number;
}

export interface AdminUser {
  id: string;
  fullName: string;
  email: string;
  userName: string;
  profilePictureUrl?: string;
  isActive: boolean;
  isSuspended: boolean;
  suspensionReason?: string;
  emailConfirmed: boolean;
  createdAt: string;
  lastSeen?: string;
  roles: string[];
}

export interface AdminPost {
  id: string;
  content?: string;
  authorName: string;
  authorId: string;
  createdAt: string;
  reactionCount: number;
  commentCount: number;
  isDeleted: boolean;
}
