using CivisOS.Application.Common.Models;
using CivisOS.Application.Messaging.DTOs;

namespace CivisOS.Application.Messaging.Interfaces;

public interface IMessagingService
{
    Task<ApiResponse<PagedResult<ConversationDto>>> GetConversationsAsync(string userId, ConversationListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<ConversationDto>> CreateConversationAsync(string userId, CreateConversationRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<MessageDto>>> GetMessagesAsync(string userId, Guid conversationId, MessageListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<MessageDto>> SendMessageAsync(string userId, Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken = default);
}
