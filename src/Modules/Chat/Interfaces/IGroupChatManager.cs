using LinkUp.Modules.Chat.DTOs;

namespace LinkUp.Modules.Chat.Interfaces;

public interface IGroupChatManager
{
    Task<GroupChatDetailsDto> CreateGroupAsync(Guid creatorId, CreateGroupChatDto dto, CancellationToken ct = default);
    Task<GroupChatDetailsDto> UpdateGroupAsync(Guid userId, Guid chatId, UpdateGroupChatDto dto, CancellationToken ct = default);
    Task AddMembersAsync(Guid userId, Guid chatId, AddGroupMembersDto dto, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid userId, Guid chatId, Guid memberId, CancellationToken ct = default);
    Task AssignAdminAsync(Guid userId, Guid chatId, Guid memberId, CancellationToken ct = default);
    Task LeaveGroupAsync(Guid userId, Guid chatId, CancellationToken ct = default);
    Task ChangeGroupPhotoAsync(Guid userId, Guid chatId, string photoUrl, CancellationToken ct = default);
    Task<GroupChatDetailsDto> GetGroupInfoAsync(Guid chatId, Guid userId, CancellationToken ct = default);
}
