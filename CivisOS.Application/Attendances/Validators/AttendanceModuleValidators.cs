using CivisOS.Application.Attendances.DTOs;
using CivisOS.Domain.Enums;
using FluentValidation;

namespace CivisOS.Application.Attendances.Validators;

public class CreateShiftRequestValidator : AbstractValidator<CreateShiftRequest>
{
    public CreateShiftRequestValidator()
    {
        RuleFor(x => x.ShiftCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ShiftName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.GracePeriodMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumWorkingMinutes).GreaterThan(0);
        RuleFor(x => x.AllowedBreakMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LateAfterMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EarlyLeavingAfterMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OvertimeAfterMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpdateShiftRequestValidator : AbstractValidator<UpdateShiftRequest>
{
    public UpdateShiftRequestValidator()
    {
        RuleFor(x => x.ShiftName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.GracePeriodMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumWorkingMinutes).GreaterThan(0);
        RuleFor(x => x.AllowedBreakMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class AssignShiftRequestValidator : AbstractValidator<AssignShiftRequest>
{
    public AssignShiftRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ShiftId).NotEmpty();
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue);
    }
}

public class BulkAssignShiftRequestValidator : AbstractValidator<BulkAssignShiftRequest>
{
    public BulkAssignShiftRequestValidator()
    {
        RuleFor(x => x.EmployeeIds).NotEmpty();
        RuleFor(x => x.ShiftId).NotEmpty();
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

public class CreatePunchRequestValidator : AbstractValidator<CreatePunchRequest>
{
    public CreatePunchRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.PunchDateTimeUtc).NotEmpty();
        RuleFor(x => x.PunchType).IsInEnum();
        RuleFor(x => x.Source).IsInEnum();
        RuleFor(x => x.DeviceId).MaximumLength(100);
        RuleFor(x => x.IpAddress).MaximumLength(64);
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
    }
}

public class SelfPunchRequestValidator : AbstractValidator<SelfPunchRequest>
{
    public SelfPunchRequestValidator()
    {
        RuleFor(x => x.PunchType).IsInEnum();
        RuleFor(x => x.Source).IsInEnum();
        RuleFor(x => x.DeviceId).MaximumLength(100);
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
    }
}

public class ManualAttendanceCorrectionRequestValidator : AbstractValidator<ManualAttendanceCorrectionRequest>
{
    public ManualAttendanceCorrectionRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.AttendanceDate).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.WorkingMinutes).GreaterThanOrEqualTo(0).When(x => x.WorkingMinutes.HasValue);
        RuleFor(x => x.LateMinutes).GreaterThanOrEqualTo(0).When(x => x.LateMinutes.HasValue);
        RuleFor(x => x.EarlyOutMinutes).GreaterThanOrEqualTo(0).When(x => x.EarlyOutMinutes.HasValue);
        RuleFor(x => x.OvertimeMinutes).GreaterThanOrEqualTo(0).When(x => x.OvertimeMinutes.HasValue);
    }
}

public class CreateRegularizationRequestValidator : AbstractValidator<CreateRegularizationRequest>
{
    public CreateRegularizationRequestValidator()
    {
        RuleFor(x => x.AttendanceDate).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Remarks).MaximumLength(1000);
        RuleFor(x => x.AttachmentReference).MaximumLength(500);
        RuleFor(x => x)
            .Must(x => x.RequestedInUtc.HasValue || x.RequestedOutUtc.HasValue)
            .WithMessage("At least one of RequestedInUtc or RequestedOutUtc is required.");
    }
}

public class CreateOvertimeRequestValidator : AbstractValidator<CreateOvertimeRequest>
{
    public CreateOvertimeRequestValidator()
    {
        RuleFor(x => x.OvertimeDate).NotEmpty();
        RuleFor(x => x.RequestedHours).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

public class ApproveOvertimeRequestValidator : AbstractValidator<ApproveOvertimeRequest>
{
    public ApproveOvertimeRequestValidator()
    {
        RuleFor(x => x.ApprovedHours).GreaterThan(0).When(x => x.ApprovedHours.HasValue);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

public class ResolveExceptionRequestValidator : AbstractValidator<ResolveExceptionRequest>
{
    public ResolveExceptionRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum()
            .Must(x => x is AttendanceExceptionStatus.Resolved or AttendanceExceptionStatus.Ignored)
            .WithMessage("Status must be Resolved or Ignored.");
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

public class MonthActionRequestValidator : AbstractValidator<MonthActionRequest>
{
    public MonthActionRequestValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}

public class UnlockMonthRequestValidator : AbstractValidator<UnlockMonthRequest>
{
    public UnlockMonthRequestValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class ProcessAttendanceRequestValidator : AbstractValidator<ProcessAttendanceRequest>
{
    public ProcessAttendanceRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.To >= x.From)
            .WithMessage("To cannot be earlier than From.");
    }
}
