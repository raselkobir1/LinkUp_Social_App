using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Friend.DTOs;

public class UserCardDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }
}

public class SendFriendRequestDto
{
    public Guid ReceiverId { get; set; }
}

public class FriendRequestDto
{
    public Guid Id { get; set; }
    public UserCardDto Sender { get; set; } = null!;
    public UserCardDto Receiver { get; set; } = null!;
    public FriendRequestStatus Status { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public int MutualFriendCount { get; set; }

    // Flat fields the Angular client reads.
    public Guid SenderId => Sender?.Id ?? Guid.Empty;
    public string SenderName => Sender?.FullName ?? string.Empty;
    public string? SenderProfilePicture => Sender?.ProfilePictureUrl;
    public Guid ReceiverId => Receiver?.Id ?? Guid.Empty;
    public string ReceiverName => Receiver?.FullName ?? string.Empty;
}

public class FriendDto
{
    public Guid UserId { get; set; }
    public UserCardDto FriendInfo { get; set; } = null!;
    public DateTime FriendSince { get; set; }
    public int MutualFriendCount { get; set; }

    // Flat fields the Angular client reads.
    public string FullName => FriendInfo?.FullName ?? string.Empty;
    public string? ProfilePictureUrl => FriendInfo?.ProfilePictureUrl;
    public DateTime FriendsSince => FriendSince;
}

public class FriendshipStatusDto
{
    public FriendshipStatus Status { get; set; }
    public Guid? RequestId { get; set; }
}
