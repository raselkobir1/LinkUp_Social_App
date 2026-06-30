export interface FriendDto {
  id: string;
  userId: string;
  fullName: string;
  userName: string;
  profilePictureUrl?: string;
  mutualFriendCount: number;
  friendsSince: string;
}

export interface FriendRequestDto {
  id: string;
  senderId: string;
  senderName: string;
  senderProfilePicture?: string;
  receiverId: string;
  receiverName: string;
  status: string;
  sentAt: string;
  mutualFriendCount: number;
}

export interface MutualFriendDto {
  id: string;
  fullName: string;
  profilePictureUrl?: string;
}

export interface BlockedUserDto {
  id: string;
  fullName: string;
  profilePictureUrl?: string;
  coverPhotoUrl?: string;
}
