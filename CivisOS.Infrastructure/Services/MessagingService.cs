using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Messaging.DTOs;
using CivisOS.Application.Messaging.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using CivisOS.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class MessagingService : IMessagingService
{
    private readonly IApplicationDbContext _db;
    private readonly IHubContext<ChatHub> _hub;

    public MessagingService(IApplicationDbContext db, IHubContext<ChatHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<ApiResponse<PagedResult<ConversationDto>>> GetConversationsAsync(
        string userId,
        ConversationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var conversationIds = _db.ConversationMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.ConversationId);

        var rows = _db.Conversations.AsNoTracking()
            .Where(c => conversationIds.Contains(c.Id));

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .OrderByDescending(c => c.UpdatedAtUtc ?? c.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var items = new List<ConversationDto>();
        foreach (var id in page)
        {
            var dto = await MapConversationAsync(id, cancellationToken);
            if (dto is not null)
            {
                items.Add(dto);
            }
        }

        return ApiResponse<PagedResult<ConversationDto>>.Ok(
            PagedResult<ConversationDto>.Create(items, total, pageNumber, pageSize));
    }

    public async Task<ApiResponse<ConversationDto>> CreateConversationAsync(
        string userId,
        CreateConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        var members = request.MemberUserIds
            .Append(userId)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (request.Type == ConversationType.Private && members.Count != 2)
        {
            return ApiResponse<ConversationDto>.Fail("Private conversations require exactly one other member.");
        }

        if (request.Type == ConversationType.Private)
        {
            var otherUserId = members.First(m => m != userId);
            var existing = await _db.Conversations.AsNoTracking()
                .Where(c => c.Type == ConversationType.Private)
                .Where(c => c.Members.Any(m => m.UserId == userId) && c.Members.Any(m => m.UserId == otherUserId))
                .Where(c => c.Members.Count() == 2)
                .Select(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing != Guid.Empty)
            {
                return ApiResponse<ConversationDto>.Ok(
                    (await MapConversationAsync(existing, cancellationToken))!,
                    "Existing private conversation returned.");
            }
        }

        var conversation = new Conversation
        {
            Title = request.Title.Trim(),
            Type = request.Type,
            CreatedByUserId = userId
        };

        _db.Add(conversation);
        foreach (var memberId in members)
        {
            _db.Add(new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = memberId,
                IsAdmin = memberId == userId,
                JoinedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<ConversationDto>.Ok(
            (await MapConversationAsync(conversation.Id, cancellationToken))!,
            "Conversation created.");
    }

    public async Task<ApiResponse<PagedResult<MessageDto>>> GetMessagesAsync(
        string userId,
        Guid conversationId,
        MessageListQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!await IsMemberAsync(userId, conversationId, cancellationToken))
        {
            return ApiResponse<PagedResult<MessageDto>>.Fail("You are not a member of this conversation.");
        }

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 50 : query.PageSize;

        var rows = _db.Messages.AsNoTracking().Where(m => m.ConversationId == conversationId);
        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(m => m.SentAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageDto(m.Id, m.ConversationId, m.SenderUserId, m.Body, m.SentAtUtc))
            .ToListAsync(cancellationToken);

        items.Reverse();
        return ApiResponse<PagedResult<MessageDto>>.Ok(
            PagedResult<MessageDto>.Create(items, total, pageNumber, pageSize));
    }

    public async Task<ApiResponse<MessageDto>> SendMessageAsync(
        string userId,
        Guid conversationId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await IsMemberAsync(userId, conversationId, cancellationToken))
        {
            return ApiResponse<MessageDto>.Fail("You are not a member of this conversation.");
        }

        var conversation = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null)
        {
            return ApiResponse<MessageDto>.Fail("Conversation not found.");
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderUserId = userId,
            Body = request.Body.Trim(),
            SentAtUtc = DateTime.UtcNow
        };

        conversation.UpdatedAtUtc = DateTime.UtcNow;
        _db.Add(message);
        _db.Update(conversation);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new MessageDto(message.Id, message.ConversationId, message.SenderUserId, message.Body, message.SentAtUtc);
        await _hub.Clients.Group(ChatHub.ConversationGroup(conversationId))
            .SendAsync("messageReceived", dto, cancellationToken);

        return ApiResponse<MessageDto>.Ok(dto, "Message sent.");
    }

    private async Task<bool> IsMemberAsync(string userId, Guid conversationId, CancellationToken cancellationToken)
    {
        return await _db.ConversationMembers.AnyAsync(
            m => m.ConversationId == conversationId && m.UserId == userId,
            cancellationToken);
    }

    private async Task<ConversationDto?> MapConversationAsync(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _db.Conversations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (conversation is null)
        {
            return null;
        }

        var members = await _db.ConversationMembers.AsNoTracking()
            .Where(m => m.ConversationId == id)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var last = await _db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == id)
            .OrderByDescending(m => m.SentAtUtc)
            .Select(m => new { m.SentAtUtc, m.Body })
            .FirstOrDefaultAsync(cancellationToken);

        return new ConversationDto(
            conversation.Id,
            conversation.Title,
            conversation.Type,
            conversation.CreatedByUserId,
            members,
            last?.SentAtUtc,
            last?.Body,
            conversation.CreatedAtUtc);
    }
}
