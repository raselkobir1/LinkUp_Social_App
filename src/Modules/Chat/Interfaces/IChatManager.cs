using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.Modules.Chat.DTOs;

namespace LinkUp.Modules.Chat.Interfaces;

public interface IChatManager
{
    Task<ChatListItemDto> GetOrCreateDirectChatAsync(Guid userId1, Guid userId2, CancellationToken ct = default);
    Task<MessageDto> SendMessageAsync(Guid senderId, SendMessageDto dto, CancellationToken ct = default);
    Task<MessageDto> EditMessageAsync(Guid userId, Guid messageId, UpdateMessageDto dto, CancellationToken ct = default);
    Task DeleteForMeAsync(Guid userId, Guid messageId, CancellationToken ct = default);
    Task DeleteForEveryoneAsync(Guid userId, Guid messageId, CancellationToken ct = default);
    Task<List<ChatListItemDto>> GetChatListAsync(Guid userId, CancellationToken ct = default);
    Task<PagedResult<MessageDto>> GetMessagesAsync(Guid chatId, Guid userId, PagedRequest request, CancellationToken ct = default);
    Task MarkDeliveredAsync(Guid messageId, Guid userId, CancellationToken ct = default);
    Task MarkReadAsync(Guid messageId, Guid userId, CancellationToken ct = default);
    Task<PagedResult<MessageDto>> SearchMessagesAsync(Guid chatId, string query, PagedRequest request, CancellationToken ct = default);
}
