using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Chat.DTOs;

public record CreateDirectChatDto(Guid TargetUserId);

public record CreateGroupChatDto(string Name, string? Description, List<Guid> MemberIds);

public record UpdateGroupChatDto(string Name, string? Description);

public record AddGroupMembersDto(List<Guid> UserIds);

public class MessageSenderDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

public record SendMessageDto(
    Guid ChatId,
    string? Content,
    MessageType MessageType,
    Guid? ReplyToMessageId = null,
    string? AttachmentUrl = null,
    string? AttachmentType = null);

public record UpdateMessageDto(string Content);

public class MessageAttachmentDto
{
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }
}

public class MessageReadDto
{
    public Guid UserId { get; set; }
    public DateTime ReadAt { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public string? Content { get; set; }
    public MessageType MessageType { get; set; }
    public MessageStatus Status { get; set; }
    public bool IsDeletedForSender { get; set; }
    public bool IsDeletedForEveryone { get; set; }
    public DateTime? EditedAt { get; set; }
    public MessageSenderDto Sender { get; set; } = new();
    public MessageDto? ReplyTo { get; set; }
    public List<MessageAttachmentDto> Attachments { get; set; } = [];
    public List<MessageReadDto> Reads { get; set; } = [];
    public DateTime CreatedAt { get; set; }

    // Flat fields consumed by the Angular client (derived from Sender).
    public Guid SenderId => Sender.Id;
    public string SenderName => Sender.FullName;
    public string? SenderProfilePicture => Sender.ProfilePictureUrl;
}

public class ChatListItemDto
{
    public Guid Id { get; set; }
    public bool IsGroup { get; set; }
    public string? GroupName { get; set; }
    public string? GroupPhotoUrl { get; set; }
    public List<MessageSenderDto> Participants { get; set; } = [];
    // Flat fields the Angular client binds to (populated for direct chats).
    public Guid? OtherUserId { get; set; }
    public string? OtherUserName { get; set; }
    public string? OtherUserProfilePicture { get; set; }
    public bool OtherUserIsOnline { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

public class GroupChatDetailsDto
{
    public Guid ChatId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GroupPhotoUrl { get; set; }
    public Guid CreatedById { get; set; }
    public List<ChatParticipantDto> Participants { get; set; } = [];
}

public class ChatParticipantDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
}
