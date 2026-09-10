using CivisOS.Application.Leaves.DTOs;
using FluentValidation;

namespace CivisOS.Application.Leaves.Validators;

public class CreateLeaveTypeRequestValidator : AbstractValidator<CreateLeaveTypeRequest>
{
    public CreateLeaveTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpdateLeaveTypeRequestValidator : AbstractValidator<UpdateLeaveTypeRequest>
{
    public UpdateLeaveTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CreateLeaveRequestDtoValidator : AbstractValidator<CreateLeaveRequestDto>
{
    public CreateLeaveRequestDtoValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.FromDate).NotEmpty();
        RuleFor(x => x.ToDate).NotEmpty()
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate cannot be earlier than FromDate.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x)
            .Must(x => !x.IsHalfDay || x.FromDate == x.ToDate)
            .WithMessage("Half-day leave must be for a single date.");
    }
}

public class LeaveApprovalRequestValidator : AbstractValidator<LeaveApprovalRequest>
{
    public LeaveApprovalRequestValidator()
    {
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}
