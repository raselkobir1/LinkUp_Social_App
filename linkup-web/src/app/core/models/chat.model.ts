export type MessageType = 'Text' | 'Image' | 'Video' | 'File' | 'Voice';
export type MessageStatus = 'Sent' | 'Delivered' | 'Read';

export interface ChatListDto {
  id: string;
  isGroup: boolean;
  groupName?: string;
  groupPhotoUrl?: string;
  otherUserId?: string;
  otherUserName?: string;
  otherUserProfilePicture?: string;
  otherUserIsOnline?: boolean;
  lastMessage?: string;
  lastMessageAt?: string;
  lastMessageSenderId?: string;
  unreadCount: number;
}

export interface MessageDto {
  id: string;
  chatId: string;
  senderId: string;
  senderName: string;
  senderProfilePicture?: string;
  content: string;
  messageType: MessageType;
  status: MessageStatus;
  replyToMessageId?: string;
  replyToMessage?: MessageDto;
  attachments: MessageAttachmentDto[];
  isDeletedForEveryone: boolean;
  editedAt?: string;
  createdAt: string;
  readBy: string[];
}

export interface MessageAttachmentDto {
  id: string;
  url: string;
  attachmentType: string;
}

export interface SendMessageDto {
  chatId: string;
  content: string;
  messageType: MessageType;
  replyToMessageId?: string;
  attachmentUrls?: string[];
}

export interface GroupChatDto {
  id: string;
  chatId: string;
  name: string;
  description?: string;
  groupPhotoUrl?: string;
  createdById: string;
  memberCount: number;
  members: GroupMemberDto[];
}

export interface GroupMemberDto {
  userId: string;
  fullName: string;
  profilePictureUrl?: string;
  isAdmin: boolean;
  joinedAt: string;
}
