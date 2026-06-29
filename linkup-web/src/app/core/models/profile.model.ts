export interface UserProfileDto {
  userId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  userName: string;
  email: string;
  bio?: string;
  gender?: string;
  dateOfBirth?: string;
  location?: string;
  website?: string;
  profilePictureUrl?: string;
  coverPhotoUrl?: string;
  isOnline: boolean;
  lastSeen?: string;
  friendCount: number;
  postCount: number;
  mutualFriendCount: number;
  friendshipStatus: FriendshipStatus;
  education: EducationDto[];
  experience: ExperienceDto[];
  socialLinks: SocialLinkDto[];
}

export type FriendshipStatus = 'None' | 'Pending' | 'RequestReceived' | 'Friends' | 'Blocked';

export interface UpdateProfileDto {
  firstName: string;
  lastName: string;
  bio?: string;
  gender?: string;
  dateOfBirth?: string;
  location?: string;
  website?: string;
}

export interface EducationDto {
  id?: string;
  school: string;
  degree?: string;
  fieldOfStudy?: string;
  startYear?: number;
  endYear?: number;
  isCurrent: boolean;
}

export interface ExperienceDto {
  id?: string;
  company: string;
  position: string;
  startDate?: string;
  endDate?: string;
  isCurrent: boolean;
  description?: string;
}

export interface SocialLinkDto {
  id?: string;
  platform: string;
  url: string;
}

export interface PrivacySettingsDto {
  profileVisibility: string;
  friendListVisibility: string;
  postDefaultVisibility: string;
}
