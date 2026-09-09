using CivisOS.Application.Cleanings.DTOs;
using CivisOS.Application.Cleanings.Interfaces;
using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class CleaningService : ICleaningService
{
    private readonly IApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public CleaningService(IApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<ApiResponse<PagedResult<CleaningAreaDto>>> GetAreasAsync(
        CleaningAreaListQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        var rows = _db.CleaningAreas.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            rows = rows.Where(a => a.Name.Contains(term) || (a.Description != null && a.Description.Contains(term)));
        }

        if (query.Status.HasValue)
        {
            rows = rows.Where(a => a.Status == query.Status.Value);
        }

        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderBy(a => a.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new CleaningAreaDto(
                a.Id,
                a.Name,
                a.Description,
                a.GeoFenceId,
                a.GeoFence == null ? null : a.GeoFence.Name,
                a.Frequency,
                a.AssignedEmployeeId,
                a.AssignedEmployee == null ? null : a.AssignedEmployee.FirstName + " " + a.AssignedEmployee.LastName,
                a.LastCleanedAtUtc,
                a.NextCleanDueAtUtc,
                a.Status,
                a.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<CleaningAreaDto>>.Ok(
            PagedResult<CleaningAreaDto>.Create(items, total, pageNumber, pageSize));
    }

    public async Task<ApiResponse<CleaningAreaDto>> GetAreaByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var area = await _db.CleaningAreas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (area is null)
        {
            return ApiResponse<CleaningAreaDto>.Fail("Cleaning area not found.");
        }

        return ApiResponse<CleaningAreaDto>.Ok(await MapAreaByIdAsync(id, cancellationToken));
    }

    public async Task<ApiResponse<CleaningAreaDto>> CreateAreaAsync(
        CreateCleaningAreaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await _db.CleaningAreas.AnyAsync(a => a.Name == request.Name, cancellationToken))
        {
            return ApiResponse<CleaningAreaDto>.Fail("A cleaning area with this name already exists.");
        }

        var fenceError = await ValidateFenceAsync(request.GeoFenceId, cancellationToken);
        if (fenceError is not null)
        {
            return ApiResponse<CleaningAreaDto>.Fail(fenceError);
        }

        var employeeError = await ValidateEmployeeAsync(request.AssignedEmployeeId, cancellationToken);
        if (employeeError is not null)
        {
            return ApiResponse<CleaningAreaDto>.Fail(employeeError);
        }

        var area = new CleaningArea
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            GeoFenceId = request.GeoFenceId,
            Frequency = request.Frequency,
            AssignedEmployeeId = request.AssignedEmployeeId,
            NextCleanDueAtUtc = DateTime.UtcNow.AddDays(FrequencyToDays(request.Frequency))
        };

        _db.Add(area);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<CleaningAreaDto>.Ok(await MapAreaByIdAsync(area.Id, cancellationToken), "Cleaning area created.");
    }

    public async Task<ApiResponse<CleaningAreaDto>> UpdateAreaAsync(
        Guid id,
        UpdateCleaningAreaRequest request,
        CancellationToken cancellationToken = default)
    {
        var area = await _db.CleaningAreas.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (area is null)
        {
            return ApiResponse<CleaningAreaDto>.Fail("Cleaning area not found.");
        }

        if (await _db.CleaningAreas.AnyAsync(a => a.Name == request.Name && a.Id != id, cancellationToken))
        {
            return ApiResponse<CleaningAreaDto>.Fail("A cleaning area with this name already exists.");
        }

        var fenceError = await ValidateFenceAsync(request.GeoFenceId, cancellationToken);
        if (fenceError is not null)
        {
            return ApiResponse<CleaningAreaDto>.Fail(fenceError);
        }

        var employeeError = await ValidateEmployeeAsync(request.AssignedEmployeeId, cancellationToken);
        if (employeeError is not null)
        {
            return ApiResponse<CleaningAreaDto>.Fail(employeeError);
        }

        area.Name = request.Name.Trim();
        area.Description = request.Description;
        area.GeoFenceId = request.GeoFenceId;
        area.Frequency = request.Frequency;
        area.AssignedEmployeeId = request.AssignedEmployeeId;
        area.Status = request.Status;
        area.UpdatedAtUtc = DateTime.UtcNow;

        _db.Update(area);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<CleaningAreaDto>.Ok(await MapAreaByIdAsync(area.Id, cancellationToken), "Cleaning area updated.");
    }

    public async Task<ApiResponse<PagedResult<CleaningScheduleDto>>> GetSchedulesAsync(
        int pageNumber,
        int pageSize,
        Guid? cleaningAreaId,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var rows = _db.CleaningSchedules.AsNoTracking().AsQueryable();
        if (cleaningAreaId.HasValue)
        {
            rows = rows.Where(s => s.CleaningAreaId == cleaningAreaId.Value);
        }

        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new CleaningScheduleDto(
                s.Id,
                s.CleaningAreaId,
                s.CleaningArea.Name,
                s.Frequency,
                s.PreferredTimeLocal,
                s.AssignedEmployeeId,
                s.AssignedEmployee == null ? null : s.AssignedEmployee.FirstName + " " + s.AssignedEmployee.LastName,
                s.IsActive,
                s.Notes,
                s.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return ApiResponse<PagedResult<CleaningScheduleDto>>.Ok(
            PagedResult<CleaningScheduleDto>.Create(items, total, pageNumber, pageSize));
    }

    public async Task<ApiResponse<CleaningScheduleDto>> CreateScheduleAsync(
        CreateCleaningScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var area = await _db.CleaningAreas.FirstOrDefaultAsync(a => a.Id == request.CleaningAreaId, cancellationToken);
        if (area is null)
        {
            return ApiResponse<CleaningScheduleDto>.Fail("Cleaning area not found.");
        }

        var employeeError = await ValidateEmployeeAsync(request.AssignedEmployeeId, cancellationToken);
        if (employeeError is not null)
        {
            return ApiResponse<CleaningScheduleDto>.Fail(employeeError);
        }

        var schedule = new CleaningSchedule
        {
            CleaningAreaId = request.CleaningAreaId,
            Frequency = request.Frequency,
            PreferredTimeLocal = request.PreferredTimeLocal,
            AssignedEmployeeId = request.AssignedEmployeeId ?? area.AssignedEmployeeId,
            Notes = request.Notes
        };

        _db.Add(schedule);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = await _db.CleaningSchedules.AsNoTracking()
            .Where(s => s.Id == schedule.Id)
            .Select(s => new CleaningScheduleDto(
                s.Id,
                s.CleaningAreaId,
                s.CleaningArea.Name,
                s.Frequency,
                s.PreferredTimeLocal,
                s.AssignedEmployeeId,
                s.AssignedEmployee == null ? null : s.AssignedEmployee.FirstName + " " + s.AssignedEmployee.LastName,
                s.IsActive,
                s.Notes,
                s.CreatedAtUtc))
            .FirstAsync(cancellationToken);

        return ApiResponse<CleaningScheduleDto>.Ok(dto, "Cleaning schedule created.");
    }

    public async Task<ApiResponse<PagedResult<CleaningLogDto>>> GetLogsAsync(
        CleaningLogListQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        var rows = _db.CleaningLogs.AsNoTracking().AsQueryable();
        if (query.CleaningAreaId.HasValue)
        {
            rows = rows.Where(l => l.CleaningAreaId == query.CleaningAreaId.Value);
        }

        if (query.EmployeeId.HasValue)
        {
            rows = rows.Where(l => l.EmployeeId == query.EmployeeId.Value);
        }

        if (query.Status.HasValue)
        {
            rows = rows.Where(l => l.Status == query.Status.Value);
        }

        if (query.From.HasValue)
        {
            rows = rows.Where(l => l.StartedAtUtc >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            rows = rows.Where(l => l.StartedAtUtc <= query.To.Value);
        }

        var total = await rows.CountAsync(cancellationToken);
        var pageIds = await rows
            .OrderByDescending(l => l.StartedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var items = new List<CleaningLogDto>();
        foreach (var id in pageIds)
        {
            var dto = await MapLogByIdAsync(id, cancellationToken);
            if (dto is not null)
            {
                items.Add(dto);
            }
        }

        return ApiResponse<PagedResult<CleaningLogDto>>.Ok(
            PagedResult<CleaningLogDto>.Create(items, total, pageNumber, pageSize));
    }

    public async Task<ApiResponse<CleaningLogDto>> CreateLogAsync(
        string userId,
        CreateCleaningLogRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);
        if (employee is null)
        {
            return ApiResponse<CleaningLogDto>.Fail("No employee profile is linked to this user account.");
        }

        var area = await _db.CleaningAreas.FirstOrDefaultAsync(a => a.Id == request.CleaningAreaId, cancellationToken);
        if (area is null)
        {
            return ApiResponse<CleaningLogDto>.Fail("Cleaning area not found.");
        }

        if (area.Status != CleaningAreaStatus.Active)
        {
            return ApiResponse<CleaningLogDto>.Fail("Cleaning area is inactive.");
        }

        var log = new CleaningLog
        {
            CleaningAreaId = area.Id,
            EmployeeId = employee.Id,
            StartedAtUtc = DateTime.UtcNow,
            Status = CleaningLogStatus.Started,
            Notes = request.Notes,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _db.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<CleaningLogDto>.Ok(await MapLogByIdAsync(log.Id, cancellationToken), "Cleaning log created.");
    }

    public async Task<ApiResponse<CleaningLogDto>> CompleteLogAsync(
        Guid id,
        CompleteCleaningLogRequest request,
        CancellationToken cancellationToken = default)
    {
        var log = await _db.CleaningLogs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (log is null)
        {
            return ApiResponse<CleaningLogDto>.Fail("Cleaning log not found.");
        }

        if (log.Status == CleaningLogStatus.Completed || log.Status == CleaningLogStatus.Inspected)
        {
            return ApiResponse<CleaningLogDto>.Fail("Cleaning log is already completed.");
        }

        log.Status = CleaningLogStatus.Completed;
        log.CompletedAtUtc = DateTime.UtcNow;
        log.Notes = request.Notes ?? log.Notes;
        log.UpdatedAtUtc = DateTime.UtcNow;
        _db.Update(log);

        var area = await _db.CleaningAreas.FirstOrDefaultAsync(a => a.Id == log.CleaningAreaId, cancellationToken);
        if (area is not null)
        {
            area.LastCleanedAtUtc = log.CompletedAtUtc;
            area.NextCleanDueAtUtc = log.CompletedAtUtc.Value.AddDays(FrequencyToDays(area.Frequency));
            area.UpdatedAtUtc = DateTime.UtcNow;
            _db.Update(area);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse<CleaningLogDto>.Ok(await MapLogByIdAsync(log.Id, cancellationToken), "Cleaning log completed.");
    }

    public async Task<ApiResponse<CleaningPhotoDto>> AddPhotoAsync(
        Guid logId,
        CleaningPhotoType photoType,
        Stream fileStream,
        string fileName,
        string contentType,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        var log = await _db.CleaningLogs.FirstOrDefaultAsync(l => l.Id == logId, cancellationToken);
        if (log is null)
        {
            return ApiResponse<CleaningPhotoDto>.Fail("Cleaning log not found.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return ApiResponse<CleaningPhotoDto>.Fail("File name is required.");
        }

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/jpg" };
        if (!allowed.Contains(contentType, StringComparer.OrdinalIgnoreCase)
            && !fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<CleaningPhotoDto>.Fail("Only JPEG, PNG, or WebP images are allowed.");
        }

        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;
        var uploadsDir = Path.Combine(webRoot, "uploads", "cleaning");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = ".jpg";
        }

        var storedName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absolutePath = Path.Combine(uploadsDir, storedName);
        await using (var fs = File.Create(absolutePath))
        {
            await fileStream.CopyToAsync(fs, cancellationToken);
        }

        var relativePath = $"/uploads/cleaning/{storedName}";
        var photo = new CleaningPhoto
        {
            CleaningLogId = logId,
            PhotoType = photoType,
            FilePath = relativePath,
            Caption = caption
        };

        _db.Add(photo);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<CleaningPhotoDto>.Ok(
            new CleaningPhotoDto(photo.Id, photo.PhotoType, photo.FilePath, photo.Caption, photo.AiConfidenceScore, photo.CreatedAtUtc),
            "Photo uploaded.");
    }

    private async Task<string?> ValidateFenceAsync(Guid? geoFenceId, CancellationToken cancellationToken)
    {
        if (!geoFenceId.HasValue)
        {
            return null;
        }

        var exists = await _db.GeoFences.AnyAsync(f => f.Id == geoFenceId.Value, cancellationToken);
        return exists ? null : "Geo-fence not found.";
    }

    private async Task<string?> ValidateEmployeeAsync(Guid? employeeId, CancellationToken cancellationToken)
    {
        if (!employeeId.HasValue)
        {
            return null;
        }

        var exists = await _db.Employees.AnyAsync(e => e.Id == employeeId.Value && e.IsActive, cancellationToken);
        return exists ? null : "Assigned employee not found or inactive.";
    }

    private static int FrequencyToDays(CleaningFrequency frequency) => frequency switch
    {
        CleaningFrequency.Daily => 1,
        CleaningFrequency.Weekly => 7,
        CleaningFrequency.BiWeekly => 14,
        CleaningFrequency.Monthly => 30,
        _ => 1
    };

    private async Task<CleaningAreaDto> MapAreaByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.CleaningAreas.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new CleaningAreaDto(
                a.Id,
                a.Name,
                a.Description,
                a.GeoFenceId,
                a.GeoFence == null ? null : a.GeoFence.Name,
                a.Frequency,
                a.AssignedEmployeeId,
                a.AssignedEmployee == null ? null : a.AssignedEmployee.FirstName + " " + a.AssignedEmployee.LastName,
                a.LastCleanedAtUtc,
                a.NextCleanDueAtUtc,
                a.Status,
                a.CreatedAtUtc))
            .FirstAsync(cancellationToken);
    }

    private async Task<CleaningLogDto?> MapLogByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var log = await _db.CleaningLogs.AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new
            {
                l.Id,
                l.CleaningAreaId,
                CleaningAreaName = l.CleaningArea.Name,
                l.EmployeeId,
                EmployeeName = l.Employee.FirstName + " " + l.Employee.LastName,
                l.StartedAtUtc,
                l.CompletedAtUtc,
                l.Status,
                l.Notes,
                l.Latitude,
                l.Longitude,
                l.InspectionScore,
                l.InspectionNotes,
                l.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (log is null)
        {
            return null;
        }

        var photos = await _db.CleaningPhotos.AsNoTracking()
            .Where(p => p.CleaningLogId == id)
            .OrderBy(p => p.CreatedAtUtc)
            .Select(p => new CleaningPhotoDto(p.Id, p.PhotoType, p.FilePath, p.Caption, p.AiConfidenceScore, p.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new CleaningLogDto(
            log.Id,
            log.CleaningAreaId,
            log.CleaningAreaName,
            log.EmployeeId,
            log.EmployeeName,
            log.StartedAtUtc,
            log.CompletedAtUtc,
            log.Status,
            log.Notes,
            log.Latitude,
            log.Longitude,
            log.InspectionScore,
            log.InspectionNotes,
            photos,
            log.CreatedAtUtc);
    }
}
