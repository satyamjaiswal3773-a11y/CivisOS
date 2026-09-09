using CivisOS.Application.Common.Interfaces;
using CivisOS.Application.Common.Models;
using CivisOS.Application.GeoFences.DTOs;
using CivisOS.Application.GeoFences.Interfaces;
using CivisOS.Domain.Entities;
using CivisOS.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace CivisOS.Infrastructure.Services;

public class GeoFenceService : IGeoFenceService
{
    private readonly IApplicationDbContext _db;

    public GeoFenceService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<PagedResult<GeoFenceDto>>> GetGeoFencesAsync(
        GeoFenceListQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 100 ? 10 : query.PageSize;

        var fences = _db.GeoFences.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            fences = fences.Where(f =>
                f.Name.ToLower().Contains(term) ||
                (f.Description != null && f.Description.ToLower().Contains(term)));
        }

        if (query.Status.HasValue)
        {
            fences = fences.Where(f => f.Status == query.Status.Value);
        }

        var total = await fences.CountAsync(cancellationToken);
        var items = await fences
            .OrderBy(f => f.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = PagedResult<GeoFenceDto>.Create(
            items.Select(Map).ToList(),
            total,
            pageNumber,
            pageSize);

        return ApiResponse<PagedResult<GeoFenceDto>>.Ok(result);
    }

    public async Task<ApiResponse<GeoFenceDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fence = await _db.GeoFences
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        return fence is null
            ? ApiResponse<GeoFenceDto>.Fail("Geo-fence not found.")
            : ApiResponse<GeoFenceDto>.Ok(Map(fence));
    }

    public async Task<ApiResponse<GeoFenceDto>> CreateAsync(
        CreateGeoFenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var nameTaken = await _db.GeoFences.AnyAsync(
            f => f.Name == request.Name.Trim(),
            cancellationToken);
        if (nameTaken)
        {
            return ApiResponse<GeoFenceDto>.Fail("A geo-fence with this name already exists.");
        }

        var fence = new GeoFence
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CenterLatitude = request.CenterLatitude,
            CenterLongitude = request.CenterLongitude,
            RadiusMeters = request.RadiusMeters,
            Status = request.Status
        };

        _db.Add(fence);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<GeoFenceDto>.Ok(Map(fence), "Geo-fence created.");
    }

    public async Task<ApiResponse<GeoFenceDto>> UpdateAsync(
        Guid id,
        UpdateGeoFenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var fence = await _db.GeoFences.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (fence is null)
        {
            return ApiResponse<GeoFenceDto>.Fail("Geo-fence not found.");
        }

        var nameTaken = await _db.GeoFences.AnyAsync(
            f => f.Name == request.Name.Trim() && f.Id != id,
            cancellationToken);
        if (nameTaken)
        {
            return ApiResponse<GeoFenceDto>.Fail("A geo-fence with this name already exists.");
        }

        fence.Name = request.Name.Trim();
        fence.Description = request.Description?.Trim();
        fence.CenterLatitude = request.CenterLatitude;
        fence.CenterLongitude = request.CenterLongitude;
        fence.RadiusMeters = request.RadiusMeters;
        fence.Status = request.Status;
        fence.UpdatedAtUtc = DateTime.UtcNow;

        _db.Update(fence);
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<GeoFenceDto>.Ok(Map(fence), "Geo-fence updated.");
    }

    public async Task<ApiResponse> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fence = await _db.GeoFences.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (fence is null)
        {
            return ApiResponse.Fail("Geo-fence not found.");
        }

        _db.Remove(fence);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResponse.Ok("Geo-fence deleted.");
    }

    public async Task<ApiResponse<GeoFenceCheckResultDto>> CheckAsync(
        GeoFenceCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var fence = await _db.GeoFences
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.GeoFenceId, cancellationToken);

        if (fence is null)
        {
            return ApiResponse<GeoFenceCheckResultDto>.Fail("Geo-fence not found.");
        }

        var distance = GeoMath.DistanceMeters(
            fence.CenterLatitude,
            fence.CenterLongitude,
            request.Latitude,
            request.Longitude);

        var isInside = GeoMath.IsInsideFence(fence, request.Latitude, request.Longitude);

        var result = new GeoFenceCheckResultDto(
            fence.Id,
            fence.Name,
            request.Latitude,
            request.Longitude,
            Math.Round(distance, 2),
            fence.RadiusMeters,
            isInside);

        return ApiResponse<GeoFenceCheckResultDto>.Ok(result);
    }

    private static GeoFenceDto Map(GeoFence f) => new(
        f.Id,
        f.Name,
        f.Description,
        f.CenterLatitude,
        f.CenterLongitude,
        f.RadiusMeters,
        f.Status,
        f.CreatedAtUtc,
        f.UpdatedAtUtc);
}
