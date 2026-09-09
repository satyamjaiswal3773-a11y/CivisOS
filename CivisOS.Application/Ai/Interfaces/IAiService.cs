using CivisOS.Application.Ai.DTOs;
using CivisOS.Application.Common.Models;

namespace CivisOS.Application.Ai.Interfaces;

public interface IAiService
{
    Task<ApiResponse<PagedResult<AiAlertDto>>> GetAlertsAsync(AiAlertListQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<AiAlertDto>> AcknowledgeAlertAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<CleaningVerifyResultDto>> VerifyCleaningPhotoAsync(CleaningVerifyRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AiAssistantAnswerDto>> AskAssistantAsync(string userId, IReadOnlyList<string> roles, AiAssistantAskRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AiAlertDto>> EvaluateOverspeedAsync(Guid vehicleId, double speedKmh, CancellationToken cancellationToken = default);
    Task EvaluateRulesAsync(CancellationToken cancellationToken = default);
}
