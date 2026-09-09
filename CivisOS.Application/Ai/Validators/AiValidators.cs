using CivisOS.Application.Ai.DTOs;
using FluentValidation;

namespace CivisOS.Application.Ai.Validators;

public class CleaningVerifyValidator : AbstractValidator<CleaningVerifyRequest>
{
    public CleaningVerifyValidator()
    {
        RuleFor(x => x.CleaningPhotoId).NotEmpty();
    }
}

public class AiAssistantAskValidator : AbstractValidator<AiAssistantAskRequest>
{
    public AiAssistantAskValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(1000);
    }
}
