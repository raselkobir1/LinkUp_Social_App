export type NotificationType =
  | 'FriendRequest' | 'FriendRequestAccepted'
  | 'PostLike' | 'PostComment' | 'CommentReply' | 'Mention'
  | 'NewMessage' | 'GroupInvite' | 'VideoCall';

export interface NotificationDto {
  id: string;
  recipientId: string;
  senderId?: string;
  senderName?: string;
  senderProfilePicture?: string;
  type: NotificationType;
  entityId?: string;
  entityType?: string;
  message: string;
  isRead: boolean;
  readAt?: string;
  createdAt: string;
}
