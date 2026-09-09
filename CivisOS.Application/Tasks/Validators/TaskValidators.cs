using CivisOS.Application.Tasks.DTOs;
using CivisOS.Domain.Enums;
using FluentValidation;

namespace CivisOS.Application.Tasks.Validators;

public class CreateWorkTaskValidator : AbstractValidator<CreateWorkTaskRequest>
{
    public CreateWorkTaskValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class UpdateWorkTaskValidator : AbstractValidator<UpdateWorkTaskRequest>
{
    public UpdateWorkTaskValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class AssignWorkTaskValidator : AbstractValidator<AssignWorkTaskRequest>
{
    public AssignWorkTaskValidator()
    {
        RuleFor(x => x.AssigneeEmployeeId).NotEmpty();
    }
}

public class UpdateWorkTaskStatusValidator : AbstractValidator<UpdateWorkTaskStatusRequest>
{
    public UpdateWorkTaskStatusValidator()
    {
        RuleFor(x => x.Status).IsInEnum()
            .Must(s => s is WorkTaskStatus.Pending or WorkTaskStatus.InProgress or WorkTaskStatus.Completed or WorkTaskStatus.Cancelled or WorkTaskStatus.Overdue)
            .WithMessage("Invalid status.");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class StartWorkTaskValidator : AbstractValidator<StartWorkTaskRequest>
{
    public StartWorkTaskValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
