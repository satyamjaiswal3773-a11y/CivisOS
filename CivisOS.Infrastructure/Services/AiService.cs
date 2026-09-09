using CivisOS.Application.Ai.DTOs;
using CivisOS.Application.Ai.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.Notifications.Interfaces;
using CivisOS.Domain.Constants;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class AiService : IAiService
{
    public const double OverspeedThresholdKmh = 60d;
    public static readonly TimeSpan StationaryThreshold = TimeSpan.FromMinutes(30);

    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public AiService(IApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<ApiResponse<PagedResult<AiAlertDto>>> GetAlertsAsync(
        AiAlertListQuery query,
        CancellationToken cancellationToken = default)
    {
        await EvaluateRulesAsync(cancellationToken);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var rows = _db.AiAlerts.AsNoTracking().AsQueryable();
        if (query.Status.HasValue)
        {
            rows = rows.Where(a => a.Status == query.Status.Value);
        }

        if (query.AlertType.HasValue)
        {
            rows = rows.Where(a => a.AlertType == query.AlertType.Value);
        }

        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(a => a.DetectedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AiAlertDto(
                a.Id,
                a.AlertType,
                a.Severity,
                a.Status,
                a.Title,
                a.Message,
                a.RelatedEntityId,
                a.RelatedEntityType,
                a.DetectedAtUtc,
                a.AcknowledgedAtUtc,
                a.AcknowledgedByUserId,
                a.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<AiAlertDto>>.Ok(
            PagedResult<AiAlertDto>.Create(items, total, pageNumber, pageSize));
    }

    public async Task<ApiResponse<AiAlertDto>> AcknowledgeAlertAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var alert = await _db.AiAlerts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (alert is null)
        {
            return ApiResponse<AiAlertDto>.Fail("Alert not found.");
        }

        alert.Status = AiAlertStatus.Acknowledged;
        alert.AcknowledgedAtUtc = DateTime.UtcNow;
        alert.AcknowledgedByUserId = userId;
        alert.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(alert);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<AiAlertDto>.Ok(Map(alert), "Alert acknowledged.");
    }

    public async Task<ApiResponse<CleaningVerifyResultDto>> VerifyCleaningPhotoAsync(
        CleaningVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        var photo = await _db.CleaningPhotos.FirstOrDefaultAsync(p => p.Id == request.CleaningPhotoId, cancellationToken);
        if (photo is null)
        {
            return ApiResponse<CleaningVerifyResultDto>.Fail("Cleaning photo not found.");
        }

        // Stub verification: score from file size + photo type heuristic (no external AI dependency).
        var webRootGuess = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var absolute = photo.FilePath.StartsWith('/')
            ? Path.Combine(webRootGuess, photo.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))
            : photo.FilePath;

        double score = 55;
        if (File.Exists(absolute))
        {
            var length = new FileInfo(absolute).Length;
            score = Math.Clamp(40 + Math.Min(length / 25_000d, 45), 40, 98);
        }

        if (photo.PhotoType == CleaningPhotoType.After)
        {
            score = Math.Min(98, score + 8);
        }

        score = Math.Round(score, 2);
        var passed = score >= 70;
        photo.AiConfidenceScore = score;
        photo.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(photo);
        await _db.SaveChangesAsync(cancellationToken);

        if (!passed)
        {
            await CreateAlertIfMissingAsync(
                AiAlertType.CleaningQuality,
                AiAlertSeverity.Warning,
                "Cleaning photo quality check failed",
                $"Photo {photo.Id} scored {score}% (threshold 70%).",
                photo.Id.ToString(),
                "CleaningPhoto",
                cancellationToken);
        }

        return ApiResponse<CleaningVerifyResultDto>.Ok(new CleaningVerifyResultDto(
            photo.Id,
            score,
            passed,
            passed
                ? "Cleaning photo looks acceptable (stub verifier)."
                : "Cleaning photo may need a retake (stub verifier)."));
    }

    public async Task<ApiResponse<AiAssistantAnswerDto>> AskAssistantAsync(
        string userId,
        IReadOnlyList<string> roles,
        AiAssistantAskRequest request,
        CancellationToken cancellationToken = default)
    {
        var q = request.Question.Trim();
        var sources = new List<string>();
        string answer;

        var isAdmin = roles.Any(r =>
            r is AppRoles.SuperAdmin or AppRoles.SocietyAdmin or AppRoles.Supervisor);

        if (!isAdmin)
        {
            return ApiResponse<AiAssistantAnswerDto>.Fail("Assistant access requires Supervisor or Admin role.");
        }

        var lower = q.ToLowerInvariant();

        if (lower.Contains("attendance") || lower.Contains("present") || lower.Contains("check-in"))
        {
            var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
            var present = await _db.Attendances.AsNoTracking()
                .CountAsync(a => a.AttendanceDate >= from
                                 && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.CheckedOut),
                    cancellationToken);
            var rejected = await _db.Attendances.AsNoTracking()
                .CountAsync(a => a.AttendanceDate >= from && a.Status == AttendanceStatus.Rejected, cancellationToken);
            sources.Add("Attendances (last 7 days)");
            answer = $"In the last 7 days there were {present} successful attendance records and {rejected} rejected check-ins.";
        }
        else if (lower.Contains("vehicle") || lower.Contains("fleet") || lower.Contains("overspeed"))
        {
            var active = await _db.Vehicles.AsNoTracking()
                .CountAsync(v => v.Status == VehicleStatus.Active || v.Status == VehicleStatus.Available, cancellationToken);
            var openOverspeed = await _db.AiAlerts.AsNoTracking()
                .CountAsync(a => a.AlertType == AiAlertType.Overspeed && a.Status == AiAlertStatus.Open, cancellationToken);
            sources.Add("Vehicles");
            sources.Add("AiAlerts");
            answer = $"Fleet has {active} active/available vehicles. There are {openOverspeed} open overspeed AI alerts.";
        }
        else if (lower.Contains("clean"))
        {
            var areas = await _db.CleaningAreas.AsNoTracking().CountAsync(cancellationToken);
            var completed = await _db.CleaningLogs.AsNoTracking()
                .CountAsync(l => l.Status == CleaningLogStatus.Completed || l.Status == CleaningLogStatus.Inspected, cancellationToken);
            sources.Add("CleaningAreas");
            sources.Add("CleaningLogs");
            answer = $"There are {areas} cleaning areas and {completed} completed/inspected cleaning logs.";
        }
        else if (lower.Contains("task") || lower.Contains("work order") || lower.Contains("overdue"))
        {
            var overdue = await _db.WorkTasks.AsNoTracking()
                .CountAsync(t => t.Status == WorkTaskStatus.Overdue, cancellationToken);
            var pending = await _db.WorkTasks.AsNoTracking()
                .CountAsync(t => t.Status == WorkTaskStatus.Pending, cancellationToken);
            sources.Add("WorkTasks");
            answer = $"There are {pending} pending tasks and {overdue} overdue tasks.";
        }
        else
        {
            var openAlerts = await _db.AiAlerts.AsNoTracking()
                .CountAsync(a => a.Status == AiAlertStatus.Open, cancellationToken);
            sources.Add("AiAlerts");
            answer =
                $"I can answer questions about attendance, vehicles/fleet, cleaning, or tasks. Currently there are {openAlerts} open AI alerts. Try asking e.g. 'How many overdue tasks?'";
        }

        return ApiResponse<AiAssistantAnswerDto>.Ok(new AiAssistantAnswerDto(q, answer, sources));
    }

    public async Task<ApiResponse<AiAlertDto>> EvaluateOverspeedAsync(
        Guid vehicleId,
        double speedKmh,
        CancellationToken cancellationToken = default)
    {
        if (speedKmh < OverspeedThresholdKmh)
        {
            return ApiResponse<AiAlertDto>.Fail("Speed is below overspeed threshold.");
        }

        var vehicle = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken);
        if (vehicle is null)
        {
            return ApiResponse<AiAlertDto>.Fail("Vehicle not found.");
        }

        var alert = await CreateAlertIfMissingAsync(
            AiAlertType.Overspeed,
            AiAlertSeverity.Critical,
            "Vehicle overspeed detected",
            $"Vehicle {vehicle.Number} reported {speedKmh:0.##} km/h (threshold {OverspeedThresholdKmh} km/h).",
            vehicleId.ToString(),
            "Vehicle",
            cancellationToken,
            allowDuplicateWithinHours: 1);

        return alert is null
            ? ApiResponse<AiAlertDto>.Fail("Overspeed alert already recorded recently.")
            : ApiResponse<AiAlertDto>.Ok(Map(alert), "Overspeed alert created.");
    }

    public async Task EvaluateRulesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Overspeed from latest locations
        var speeding = await _db.VehicleLocations.AsNoTracking()
            .Where(l => l.SpeedKmh != null && l.SpeedKmh >= OverspeedThresholdKmh)
            .Select(l => new { l.VehicleId, Speed = l.SpeedKmh!.Value, Number = l.Vehicle.Number })
            .ToListAsync(cancellationToken);

        foreach (var row in speeding)
        {
            await CreateAlertIfMissingAsync(
                AiAlertType.Overspeed,
                AiAlertSeverity.Critical,
                "Vehicle overspeed detected",
                $"Vehicle {row.Number} reported {row.Speed:0.##} km/h (threshold {OverspeedThresholdKmh} km/h).",
                row.VehicleId.ToString(),
                "Vehicle",
                cancellationToken,
                allowDuplicateWithinHours: 1);
        }

        // Stationary vehicles
        var cutoff = now - StationaryThreshold;
        var stationary = await _db.VehicleLocations.AsNoTracking()
            .Where(l => l.RecordedAtUtc <= cutoff && (l.SpeedKmh == null || l.SpeedKmh < 1))
            .Select(l => new { l.VehicleId, Number = l.Vehicle.Number, l.RecordedAtUtc })
            .ToListAsync(cancellationToken);

        foreach (var row in stationary)
        {
            await CreateAlertIfMissingAsync(
                AiAlertType.Stationary,
                AiAlertSeverity.Warning,
                "Vehicle appears stationary",
                $"Vehicle {row.Number} has not moved since {row.RecordedAtUtc:u}.",
                row.VehicleId.ToString(),
                "Vehicle",
                cancellationToken,
                allowDuplicateWithinHours: 6);
        }

        // Overdue tasks
        var overdueTasks = await _db.WorkTasks
            .Where(t => t.DeadlineUtc != null
                        && t.DeadlineUtc < now
                        && (t.Status == WorkTaskStatus.Pending || t.Status == WorkTaskStatus.InProgress || t.Status == WorkTaskStatus.Overdue))
            .ToListAsync(cancellationToken);

        foreach (var task in overdueTasks)
        {
            if (task.Status != WorkTaskStatus.Overdue)
            {
                task.Status = WorkTaskStatus.Overdue;
                task.UpdatedAtUtc = now;
                _db.Update(task);
            }

            await CreateAlertIfMissingAsync(
                AiAlertType.TaskOverdue,
                AiAlertSeverity.Warning,
                "Task overdue",
                $"Task '{task.Title}' is overdue (deadline {task.DeadlineUtc:u}).",
                task.Id.ToString(),
                "WorkTask",
                cancellationToken,
                allowDuplicateWithinHours: 12);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AiAlert?> CreateAlertIfMissingAsync(
        AiAlertType type,
        AiAlertSeverity severity,
        string title,
        string message,
        string relatedEntityId,
        string relatedEntityType,
        CancellationToken cancellationToken,
        int allowDuplicateWithinHours = 24)
    {
        var since = DateTime.UtcNow.AddHours(-allowDuplicateWithinHours);
        var exists = await _db.AiAlerts.AnyAsync(
            a => a.AlertType == type
                 && a.RelatedEntityId == relatedEntityId
                 && a.RelatedEntityType == relatedEntityType
                 && a.DetectedAtUtc >= since
                 && a.Status != AiAlertStatus.Resolved,
            cancellationToken);

        if (exists)
        {
            return null;
        }

        var alert = new AiAlert
        {
            AlertType = type,
            Severity = severity,
            Title = title,
            Message = message,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            DetectedAtUtc = DateTime.UtcNow,
            Status = AiAlertStatus.Open
        };

        _db.Add(alert);
        await _db.SaveChangesAsync(cancellationToken);

        var adminUserIds = await _db.Employees.AsNoTracking()
            .Where(e => e.UserId != null && e.IsActive)
            .Select(e => e.UserId!)
            .Take(20)
            .ToListAsync(cancellationToken);

        // Prefer notifying via roles is hard without Identity here; notify linked employees as ops audience.
        foreach (var uid in adminUserIds.Take(5))
        {
            await _notifications.NotifyUserAsync(
                uid,
                title,
                message,
                NotificationType.AiAlert,
                relatedEntityId,
                relatedEntityType,
                cancellationToken);
        }

        return alert;
    }

    private static AiAlertDto Map(AiAlert a) => new(
        a.Id,
        a.AlertType,
        a.Severity,
        a.Status,
        a.Title,
        a.Message,
        a.RelatedEntityId,
        a.RelatedEntityType,
        a.DetectedAtUtc,
        a.AcknowledgedAtUtc,
        a.AcknowledgedByUserId,
        a.CreatedAtUtc);
}
