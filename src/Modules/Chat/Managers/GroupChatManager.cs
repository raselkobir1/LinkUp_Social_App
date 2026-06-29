using AutoMapper;
using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.Modules.Chat.Configuration;
using LinkUp.Modules.Chat.DTOs;
using LinkUp.Modules.Chat.Entities;
using LinkUp.Modules.Chat.Interfaces;
using LinkUp.Modules.Identity.Entities;
using LinkUp.Modules.Notification.DTOs;
using LinkUp.Modules.Notification.Interfaces;
using LinkUp.SharedKernel.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LinkUp.Modules.Chat.Managers;

public class GroupChatManager(
    ChatDbContext db,
    UserManager<ApplicationUser> userManager,
    INotificationManager notificationManager,
    IMapper mapper) : IGroupChatManager
{
    private async Task NotifyGroupInviteAsync(Guid inviterId, IEnumerable<Guid> memberIds, Guid chatId, string groupName, CancellationToken ct)
    {
        var inviter = await userManager.FindByIdAsync(inviterId.ToString());
        var inviterName = inviter?.FullName ?? "Someone";
        foreach (var memberId in memberIds.Where(id => id != inviterId).Distinct())
        {
            await notificationManager.CreateNotificationAsync(new CreateNotificationDto(
                memberId, inviterId, NotificationType.GroupInvite,
                chatId, "Chat", $"{inviterName} added you to the group \"{groupName}\""), ct);
        }
    }
    // Retained for future AutoMapper-based projections
    private readonly IMapper _mapper = mapper;
    public async Task<GroupChatDetailsDto> CreateGroupAsync(
        Guid creatorId, CreateGroupChatDto dto, CancellationToken ct = default)
    {
        // Create the base Chat record
        var chat = new Entities.Chat
        {
            IsGroup = true,
            CreatedById = creatorId
        };
        db.Chats.Add(chat);

        // Create the GroupChat metadata record
        var groupChat = new GroupChat
        {
            ChatId = chat.Id,
            Name = dto.Name,
            Description = dto.Description,
            CreatedById = creatorId
        };
        db.GroupChats.Add(groupChat);

        // Add creator as an admin participant
        db.ChatParticipants.Add(new ChatParticipant
        {
            ChatId = chat.Id,
            UserId = creatorId,
            IsAdmin = true
        });

        // Add all other members as non-admin participants (skip duplicates)
        var uniqueMembers = dto.MemberIds
            .Where(id => id != creatorId)
            .Distinct();

        foreach (var memberId in uniqueMembers)
        {
            db.ChatParticipants.Add(new ChatParticipant
            {
                ChatId = chat.Id,
                UserId = memberId,
                IsAdmin = false
            });
        }

        await db.SaveChangesAsync(ct);

        await NotifyGroupInviteAsync(creatorId, uniqueMembers, chat.Id, dto.Name, ct);

        return await BuildGroupDetailsDtoAsync(groupChat, chat.Id, ct);
    }

    public async Task<GroupChatDetailsDto> UpdateGroupAsync(
        Guid userId, Guid chatId, UpdateGroupChatDto dto, CancellationToken ct = default)
    {
        await RequireAdminAsync(userId, chatId, ct);

        var groupChat = await db.GroupChats
            .FirstOrDefaultAsync(g => g.ChatId == chatId, ct)
            ?? throw new NotFoundException("GroupChat", chatId);

        groupChat.Name = dto.Name;
        groupChat.Description = dto.Description;
        groupChat.UpdatedById = userId;

        await db.SaveChangesAsync(ct);

        return await BuildGroupDetailsDtoAsync(groupChat, chatId, ct);
    }

    public async Task AddMembersAsync(
        Guid userId, Guid chatId, AddGroupMembersDto dto, CancellationToken ct = default)
    {
        await RequireAdminAsync(userId, chatId, ct);

        // Fetch existing active participant user IDs to avoid duplicates
        var existingIds = await db.ChatParticipants
            .Where(cp => cp.ChatId == chatId && cp.IsActive)
            .Select(cp => cp.UserId)
            .ToHashSetAsync(ct);

        foreach (var memberId in dto.UserIds.Distinct())
        {
            if (existingIds.Contains(memberId))
                continue;

            // Re-activate if previously left
            var inactive = await db.ChatParticipants
                .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == memberId && !cp.IsActive, ct);

            if (inactive is not null)
            {
                inactive.IsActive = true;
                inactive.LeftAt = null;
                inactive.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                db.ChatParticipants.Add(new ChatParticipant
                {
                    ChatId = chatId,
                    UserId = memberId,
                    IsAdmin = false
                });
            }
        }

        await db.SaveChangesAsync(ct);

        var groupName = await db.GroupChats
            .Where(g => g.ChatId == chatId)
            .Select(g => g.Name)
            .FirstOrDefaultAsync(ct) ?? "a group";
        var newMembers = dto.UserIds.Distinct().Where(id => !existingIds.Contains(id));
        await NotifyGroupInviteAsync(userId, newMembers, chatId, groupName, ct);
    }

    public async Task RemoveMemberAsync(
        Guid userId, Guid chatId, Guid memberId, CancellationToken ct = default)
    {
        // An admin can remove anyone; a user can always remove themselves
        if (userId != memberId)
            await RequireAdminAsync(userId, chatId, ct);

        var participant = await db.ChatParticipants
            .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == memberId && cp.IsActive, ct)
            ?? throw new NotFoundException("ChatParticipant", memberId);

        participant.IsActive = false;
        participant.LeftAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task AssignAdminAsync(
        Guid userId, Guid chatId, Guid memberId, CancellationToken ct = default)
    {
        await RequireAdminAsync(userId, chatId, ct);

        var participant = await db.ChatParticipants
            .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == memberId && cp.IsActive, ct)
            ?? throw new NotFoundException("ChatParticipant", memberId);

        participant.IsAdmin = true;

        await db.SaveChangesAsync(ct);
    }

    public async Task LeaveGroupAsync(Guid userId, Guid chatId, CancellationToken ct = default)
    {
        var participant = await db.ChatParticipants
            .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == userId && cp.IsActive, ct)
            ?? throw new NotFoundException("ChatParticipant", userId);

        participant.IsActive = false;
        participant.LeftAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task ChangeGroupPhotoAsync(
        Guid userId, Guid chatId, string photoUrl, CancellationToken ct = default)
    {
        await RequireAdminAsync(userId, chatId, ct);

        var groupChat = await db.GroupChats
            .FirstOrDefaultAsync(g => g.ChatId == chatId, ct)
            ?? throw new NotFoundException("GroupChat", chatId);

        groupChat.GroupPhotoUrl = photoUrl;
        groupChat.UpdatedById = userId;

        await db.SaveChangesAsync(ct);
    }

    public async Task<GroupChatDetailsDto> GetGroupInfoAsync(
        Guid chatId, Guid userId, CancellationToken ct = default)
    {
        var isParticipant = await db.ChatParticipants
            .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId && cp.IsActive, ct);

        if (!isParticipant)
            throw new ForbiddenException("You are not a member of this group.");

        var groupChat = await db.GroupChats
            .FirstOrDefaultAsync(g => g.ChatId == chatId, ct)
            ?? throw new NotFoundException("GroupChat", chatId);

        return await BuildGroupDetailsDtoAsync(groupChat, chatId, ct);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task RequireAdminAsync(Guid userId, Guid chatId, CancellationToken ct)
    {
        var isAdmin = await db.ChatParticipants
            .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId && cp.IsActive && cp.IsAdmin, ct);

        if (!isAdmin)
            throw new ForbiddenException("You do not have admin permissions for this group.");
    }

    private async Task<GroupChatDetailsDto> BuildGroupDetailsDtoAsync(
        GroupChat groupChat, Guid chatId, CancellationToken ct)
    {
        var participants = await db.ChatParticipants
            .Where(cp => cp.ChatId == chatId && cp.IsActive)
            .ToListAsync(ct);

        var participantDtos = new List<ChatParticipantDto>();

        foreach (var p in participants)
        {
            var user = await userManager.FindByIdAsync(p.UserId.ToString());
            participantDtos.Add(new ChatParticipantDto
            {
                UserId = p.UserId,
                FullName = user?.FullName ?? string.Empty,
                ProfilePictureUrl = user?.ProfilePictureUrl,
                JoinedAt = p.JoinedAt,
                IsAdmin = p.IsAdmin,
                IsActive = p.IsActive
            });
        }

        return new GroupChatDetailsDto
        {
            ChatId = chatId,
            Name = groupChat.Name,
            Description = groupChat.Description,
            GroupPhotoUrl = groupChat.GroupPhotoUrl,
            CreatedById = groupChat.CreatedById ?? Guid.Empty,
            Participants = participantDtos
        };
    }
}
