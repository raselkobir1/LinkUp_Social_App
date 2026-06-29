using AutoMapper;
using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.BuildingBlocks.Common.Pagination;
using LinkUp.BuildingBlocks.Infrastructure.Extensions;
using LinkUp.Modules.Chat.Configuration;
using LinkUp.Modules.Chat.DTOs;
using LinkUp.Modules.Chat.Entities;
using LinkUp.Modules.Chat.Hubs;
using LinkUp.Modules.Chat.Interfaces;
using LinkUp.Modules.Identity.Entities;
using LinkUp.SharedKernel.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Chat.Managers;

public class ChatManager(
    ChatDbContext db,
    UserManager<ApplicationUser> userManager,
    IMapper mapper,
    IHubContext<ChatHub> hubContext) : IChatManager
{
    // Retained for future AutoMapper-based projections
    private readonly IMapper _mapper = mapper;
    // -------------------------------------------------------------------------
    // Direct chat
    // -------------------------------------------------------------------------

    public async Task<ChatListItemDto> GetOrCreateDirectChatAsync(
        Guid userId1, Guid userId2, CancellationToken ct = default)
    {
        // Find an existing non-group chat where BOTH users are active participants.
        var existingChatId = await db.ChatParticipants
            .Where(cp => cp.UserId == userId1 && cp.IsActive)
            .Select(cp => cp.ChatId)
            .Intersect(
                db.ChatParticipants
                    .Where(cp => cp.UserId == userId2 && cp.IsActive)
                    .Select(cp => cp.ChatId))
            .Join(
                db.Chats.Where(c => !c.IsGroup && !c.IsDeleted),
                id => id,
                c => c.Id,
                (id, c) => id)
            .FirstOrDefaultAsync(ct);

        if (existingChatId != default)
        {
            var existingChat = await db.Chats.FirstAsync(c => c.Id == existingChatId, ct);
            return await BuildChatListItemDtoAsync(existingChat, userId1, ct);
        }

        // Create a new direct chat.
        var chat = new Entities.Chat
        {
            IsGroup = false,
            CreatedById = userId1
        };

        db.Chats.Add(chat);

        db.ChatParticipants.AddRange(
            new ChatParticipant { ChatId = chat.Id, UserId = userId1 },
            new ChatParticipant { ChatId = chat.Id, UserId = userId2 });

        await db.SaveChangesAsync(ct);

        return await BuildChatListItemDtoAsync(chat, userId1, ct);
    }

    // -------------------------------------------------------------------------
    // Messaging
    // -------------------------------------------------------------------------

    public async Task<MessageDto> SendMessageAsync(
        Guid senderId, SendMessageDto dto, CancellationToken ct = default)
    {
        var isParticipant = await db.ChatParticipants
            .AnyAsync(cp => cp.ChatId == dto.ChatId && cp.UserId == senderId && cp.IsActive, ct);

        if (!isParticipant)
            throw new ForbiddenException("You are not an active participant of this chat.");

        var chat = await db.Chats.FirstOrDefaultAsync(c => c.Id == dto.ChatId && !c.IsDeleted, ct)
            ?? throw new NotFoundException("Chat", dto.ChatId);

        var message = new Message
        {
            ChatId = dto.ChatId,
            SenderId = senderId,
            Content = dto.Content,
            MessageType = dto.MessageType,
            Status = MessageStatus.Sent,
            ReplyToMessageId = dto.ReplyToMessageId,
            CreatedById = senderId
        };

        db.Messages.Add(message);

        // Attachment (optional – single attachment from the DTO)
        if (!string.IsNullOrWhiteSpace(dto.AttachmentUrl))
        {
            db.MessageAttachments.Add(new MessageAttachment
            {
                MessageId = message.Id,
                Url = dto.AttachmentUrl,
                FileType = dto.AttachmentType ?? "application/octet-stream"
            });
        }

        // Update chat's LastMessageAt
        chat.LastMessageAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var messageDto = await BuildMessageDtoAsync(message, senderId, ct);

        // Push via SignalR
        await hubContext.Clients
            .Group(dto.ChatId.ToString())
            .SendAsync("ReceiveMessage", messageDto, ct);

        return messageDto;
    }

    public async Task<MessageDto> EditMessageAsync(
        Guid userId, Guid messageId, UpdateMessageDto dto, CancellationToken ct = default)
    {
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeletedForEveryone, ct)
            ?? throw new NotFoundException("Message", messageId);

        if (message.SenderId != userId)
            throw new ForbiddenException("You can only edit your own messages.");

        message.Content = dto.Content;
        message.EditedAt = DateTime.UtcNow;
        message.UpdatedById = userId;

        await db.SaveChangesAsync(ct);

        var messageDto = await BuildMessageDtoAsync(message, userId, ct);

        await hubContext.Clients
            .Group(message.ChatId.ToString())
            .SendAsync("MessageEdited", messageDto, ct);

        return messageDto;
    }

    public async Task DeleteForMeAsync(Guid userId, Guid messageId, CancellationToken ct = default)
    {
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId, ct)
            ?? throw new NotFoundException("Message", messageId);

        var isParticipant = await db.ChatParticipants
            .AnyAsync(cp => cp.ChatId == message.ChatId && cp.UserId == userId && cp.IsActive, ct);

        if (!isParticipant)
            throw new ForbiddenException("You are not a participant of this chat.");

        if (message.SenderId == userId)
            message.IsDeletedForSender = true;
        else
            // For recipients there is no separate flag per user in this design;
            // treat the action as "soft delete for sender" on the recipient's behalf
            // by tracking via IsDeletedForSender when sender matches.
            // Since only the sender can set IsDeletedForSender, non-sender members
            // cannot delete-for-me in this basic model — throw forbidden.
            throw new ForbiddenException("Only the message sender can delete a message for themselves in this version.");

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteForEveryoneAsync(Guid userId, Guid messageId, CancellationToken ct = default)
    {
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeletedForEveryone, ct)
            ?? throw new NotFoundException("Message", messageId);

        if (message.SenderId != userId)
            throw new ForbiddenException("Only the message sender can delete a message for everyone.");

        message.IsDeletedForEveryone = true;
        message.UpdatedById = userId;

        await db.SaveChangesAsync(ct);

        await hubContext.Clients
            .Group(message.ChatId.ToString())
            .SendAsync("MessageDeleted", messageId, ct);
    }

    // -------------------------------------------------------------------------
    // Chat list and message retrieval
    // -------------------------------------------------------------------------

    public async Task<List<ChatListItemDto>> GetChatListAsync(
        Guid userId, CancellationToken ct = default)
    {
        var chatIds = await db.ChatParticipants
            .Where(cp => cp.UserId == userId && cp.IsActive)
            .Select(cp => cp.ChatId)
            .ToListAsync(ct);

        var chats = await db.Chats
            .Where(c => chatIds.Contains(c.Id) && !c.IsDeleted)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(ct);

        var result = new List<ChatListItemDto>();

        foreach (var chat in chats)
            result.Add(await BuildChatListItemDtoAsync(chat, userId, ct));

        return result;
    }

    public async Task<PagedResult<MessageDto>> GetMessagesAsync(
        Guid chatId, Guid userId, PagedRequest request, CancellationToken ct = default)
    {
        var isParticipant = await db.ChatParticipants
            .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId && cp.IsActive, ct);

        if (!isParticipant)
            throw new ForbiddenException("You are not an active participant of this chat.");

        var query = db.Messages
            .Where(m => m.ChatId == chatId
                && !m.IsDeletedForEveryone
                && !(m.SenderId == userId && m.IsDeletedForSender))
            .OrderByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var messages = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = new List<MessageDto>();
        foreach (var m in messages)
            dtos.Add(await BuildMessageDtoAsync(m, userId, ct));

        return PagedResult<MessageDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize);
    }

    // -------------------------------------------------------------------------
    // Status tracking
    // -------------------------------------------------------------------------

    public async Task MarkDeliveredAsync(Guid messageId, Guid userId, CancellationToken ct = default)
    {
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId, ct)
            ?? throw new NotFoundException("Message", messageId);

        if (message.Status < MessageStatus.Delivered)
        {
            message.Status = MessageStatus.Delivered;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task MarkReadAsync(Guid messageId, Guid userId, CancellationToken ct = default)
    {
        var message = await db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId, ct)
            ?? throw new NotFoundException("Message", messageId);

        // Upsert MessageRead record
        var alreadyRead = await db.MessageReads
            .AnyAsync(r => r.MessageId == messageId && r.UserId == userId, ct);

        if (!alreadyRead)
        {
            db.MessageReads.Add(new MessageRead
            {
                MessageId = messageId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            });
        }

        // Check if all active participants (excluding the sender) have read the message
        var participantIds = await db.ChatParticipants
            .Where(cp => cp.ChatId == message.ChatId && cp.IsActive && cp.UserId != message.SenderId)
            .Select(cp => cp.UserId)
            .ToListAsync(ct);

        var readUserIds = await db.MessageReads
            .Where(r => r.MessageId == messageId)
            .Select(r => r.UserId)
            .ToListAsync(ct);

        if (participantIds.All(pid => readUserIds.Contains(pid)))
            message.Status = MessageStatus.Read;
        else if (message.Status < MessageStatus.Delivered)
            message.Status = MessageStatus.Delivered;

        await db.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // Search
    // -------------------------------------------------------------------------

    public async Task<PagedResult<MessageDto>> SearchMessagesAsync(
        Guid chatId, string query, PagedRequest request, CancellationToken ct = default)
    {
        var messagesQuery = db.Messages
            .Where(m => m.ChatId == chatId
                && !m.IsDeletedForEveryone
                && m.Content != null
                && EF.Functions.ILike(m.Content, $"%{query}%"))
            .OrderByDescending(m => m.CreatedAt);

        var totalCount = await messagesQuery.CountAsync(ct);
        var messages = await messagesQuery
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = new List<MessageDto>();
        foreach (var m in messages)
            dtos.Add(await BuildMessageDtoAsync(m, Guid.Empty, ct));

        return PagedResult<MessageDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<MessageDto> BuildMessageDtoAsync(
        Message message, Guid currentUserId, CancellationToken ct)
    {
        // Sender
        var senderUser = await userManager.FindByIdAsync(message.SenderId.ToString());
        var senderDto = new MessageSenderDto
        {
            Id = message.SenderId,
            FullName = senderUser?.FullName ?? string.Empty,
            ProfilePictureUrl = senderUser?.ProfilePictureUrl
        };

        // Attachments
        var attachments = await db.MessageAttachments
            .Where(a => a.MessageId == message.Id)
            .ToListAsync(ct);

        var attachmentDtos = attachments.Select(a => new MessageAttachmentDto
        {
            Url = a.Url,
            ThumbnailUrl = a.ThumbnailUrl,
            FileType = a.FileType,
            FileName = a.FileName,
            FileSizeBytes = a.FileSizeBytes
        }).ToList();

        // Read receipts
        var reads = await db.MessageReads
            .Where(r => r.MessageId == message.Id)
            .ToListAsync(ct);

        var readDtos = reads.Select(r => new MessageReadDto
        {
            UserId = r.UserId,
            ReadAt = r.ReadAt
        }).ToList();

        // Reply-to (one level deep only to avoid infinite recursion)
        MessageDto? replyToDto = null;
        if (message.ReplyToMessageId.HasValue)
        {
            var replyTo = await db.Messages
                .FirstOrDefaultAsync(m => m.Id == message.ReplyToMessageId.Value, ct);

            if (replyTo is not null)
            {
                var replyToSender = await userManager.FindByIdAsync(replyTo.SenderId.ToString());
                replyToDto = new MessageDto
                {
                    Id = replyTo.Id,
                    ChatId = replyTo.ChatId,
                    Content = replyTo.IsDeletedForEveryone ? null : replyTo.Content,
                    MessageType = replyTo.MessageType,
                    Status = replyTo.Status,
                    IsDeletedForSender = replyTo.IsDeletedForSender,
                    IsDeletedForEveryone = replyTo.IsDeletedForEveryone,
                    EditedAt = replyTo.EditedAt,
                    CreatedAt = replyTo.CreatedAt,
                    Sender = new MessageSenderDto
                    {
                        Id = replyTo.SenderId,
                        FullName = replyToSender?.FullName ?? string.Empty,
                        ProfilePictureUrl = replyToSender?.ProfilePictureUrl
                    }
                };
            }
        }

        return new MessageDto
        {
            Id = message.Id,
            ChatId = message.ChatId,
            Content = message.IsDeletedForEveryone ? null : message.Content,
            MessageType = message.MessageType,
            Status = message.Status,
            IsDeletedForSender = message.IsDeletedForSender,
            IsDeletedForEveryone = message.IsDeletedForEveryone,
            EditedAt = message.EditedAt,
            CreatedAt = message.CreatedAt,
            Sender = senderDto,
            ReplyTo = replyToDto,
            Attachments = attachmentDtos,
            Reads = readDtos
        };
    }

    private async Task<ChatListItemDto> BuildChatListItemDtoAsync(
        Entities.Chat chat, Guid currentUserId, CancellationToken ct)
    {
        var dto = new ChatListItemDto
        {
            Id = chat.Id,
            IsGroup = chat.IsGroup
        };

        // Group info
        if (chat.IsGroup)
        {
            var groupChat = await db.GroupChats
                .FirstOrDefaultAsync(g => g.ChatId == chat.Id, ct);

            if (groupChat is not null)
            {
                dto.GroupName = groupChat.Name;
                dto.GroupPhotoUrl = groupChat.GroupPhotoUrl;
            }
        }

        // Participants
        var participantIds = await db.ChatParticipants
            .Where(cp => cp.ChatId == chat.Id && cp.IsActive)
            .Select(cp => cp.UserId)
            .ToListAsync(ct);

        var participants = new List<MessageSenderDto>();
        foreach (var pid in participantIds)
        {
            var user = await userManager.FindByIdAsync(pid.ToString());
            if (user is not null)
            {
                participants.Add(new MessageSenderDto
                {
                    Id = pid,
                    FullName = user.FullName,
                    ProfilePictureUrl = user.ProfilePictureUrl
                });

                // For direct chats, surface the "other" participant as flat fields.
                if (!chat.IsGroup && pid != currentUserId)
                {
                    dto.OtherUserId = pid;
                    dto.OtherUserName = user.FullName;
                    dto.OtherUserProfilePicture = user.ProfilePictureUrl;
                    dto.OtherUserIsOnline = user.IsOnline;
                }
            }
        }
        dto.Participants = participants;

        // Last message
        var lastMessage = await db.Messages
            .Where(m => m.ChatId == chat.Id && !m.IsDeletedForEveryone)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (lastMessage is not null)
        {
            dto.LastMessage = string.IsNullOrEmpty(lastMessage.Content)
                ? "[attachment]"
                : lastMessage.Content;
            dto.LastMessageAt = lastMessage.CreatedAt;
        }

        // Unread count (messages sent after the last MessageRead by currentUserId)
        var lastReadAt = await db.MessageReads
            .Where(r => r.UserId == currentUserId
                && db.Messages.Any(m => m.Id == r.MessageId && m.ChatId == chat.Id))
            .OrderByDescending(r => r.ReadAt)
            .Select(r => (DateTime?)r.ReadAt)
            .FirstOrDefaultAsync(ct);

        dto.UnreadCount = await db.Messages
            .CountAsync(m => m.ChatId == chat.Id
                && !m.IsDeletedForEveryone
                && m.SenderId != currentUserId
                && (lastReadAt == null || m.CreatedAt > lastReadAt.Value), ct);

        return dto;
    }
}
