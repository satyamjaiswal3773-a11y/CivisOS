using CivisOS.Domain.Enums;

namespace CivisOS.Application.Attendances.DTOs;

public record AttendanceDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid GeoFenceId,
    string GeoFenceName,
    DateOnly AttendanceDate,
    DateTime? CheckInAtUtc,
    DateTime? CheckOutAtUtc,
    double CheckInLatitude,
    double CheckInLongitude,
    double CheckInDistanceMeters,
    double? CheckOutLatitude,
    double? CheckOutLongitude,
    double? CheckOutDistanceMeters,
    AttendanceStatus Status,
    string? RejectionReason,
    DateTime CreatedAtUtc);

public record AttendanceCheckInRequest(
    double Latitude,
    double Longitude,
    Guid GeoFenceId);

public record AttendanceCheckOutRequest(
    double Latitude,
    double Longitude);

public record AttendanceListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? EmployeeId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    AttendanceStatus? Status = null);
