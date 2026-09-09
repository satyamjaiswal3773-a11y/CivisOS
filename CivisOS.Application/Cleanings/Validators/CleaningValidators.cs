using CivisOS.Application.Cleanings.DTOs;
using CivisOS.Domain.Enums;
using FluentValidation;

namespace CivisOS.Application.Cleanings.Validators;

public class CreateCleaningAreaValidator : AbstractValidator<CreateCleaningAreaRequest>
{
    public CreateCleaningAreaValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Frequency).IsInEnum();
    }
}

public class UpdateCleaningAreaValidator : AbstractValidator<UpdateCleaningAreaRequest>
{
    public UpdateCleaningAreaValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Frequency).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class CreateCleaningScheduleValidator : AbstractValidator<CreateCleaningScheduleRequest>
{
    public CreateCleaningScheduleValidator()
    {
        RuleFor(x => x.CleaningAreaId).NotEmpty();
        RuleFor(x => x.Frequency).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public class CreateCleaningLogValidator : AbstractValidator<CreateCleaningLogRequest>
{
    public CreateCleaningLogValidator()
    {
        RuleFor(x => x.CleaningAreaId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
    }
}
