using LinkUp.BuildingBlocks.Common.Entities;
using LinkUp.SharedKernel.Enums;

namespace LinkUp.Modules.Friend.Entities;

public class FriendRequest : AuditableEntity
{
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderProfilePictureUrl { get; set; }

    public Guid ReceiverId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string? ReceiverProfilePictureUrl { get; set; }

    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
    public DateTime? RespondedAt { get; set; }
}
