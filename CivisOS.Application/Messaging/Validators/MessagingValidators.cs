using CivisOS.Application.Messaging.DTOs;
using CivisOS.Domain.Enums;
using FluentValidation;

namespace CivisOS.Application.Messaging.Validators;

public class CreateConversationValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.MemberUserIds).NotNull().Must(m => m.Count > 0)
            .WithMessage("At least one member is required.");
        RuleForEach(x => x.MemberUserIds).NotEmpty().MaximumLength(450);
        RuleFor(x => x.MemberUserIds)
            .Must(m => m.Distinct(StringComparer.Ordinal).Count() == m.Count)
            .WithMessage("Duplicate members are not allowed.");
    }
}

public class SendMessageValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}
