using CivisOS.Application.Holidays.DTOs;
using CivisOS.Domain.Enums;
using FluentValidation;

namespace CivisOS.Application.Holidays.Validators;

public class CreateHolidayRequestValidator : AbstractValidator<CreateHolidayRequest>
{
    public CreateHolidayRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.HolidayDate).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpdateHolidayRequestValidator : AbstractValidator<UpdateHolidayRequest>
{
    public UpdateHolidayRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.HolidayDate).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CreateWeeklyOffRuleRequestValidator : AbstractValidator<CreateWeeklyOffRuleRequest>
{
    public CreateWeeklyOffRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Pattern).IsInEnum();
        RuleFor(x => x.FixedDaysCsv).MaximumLength(50);
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue);
        RuleFor(x => x.FixedDaysCsv)
            .NotEmpty()
            .When(x => x.Pattern is WeeklyOffPattern.FixedDays or WeeklyOffPattern.Rotational)
            .WithMessage("FixedDaysCsv is required for FixedDays and Rotational patterns.");
    }
}

public class UpdateWeeklyOffRuleRequestValidator : AbstractValidator<UpdateWeeklyOffRuleRequest>
{
    public UpdateWeeklyOffRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Pattern).IsInEnum();
        RuleFor(x => x.FixedDaysCsv).MaximumLength(50);
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue);
    }
}
