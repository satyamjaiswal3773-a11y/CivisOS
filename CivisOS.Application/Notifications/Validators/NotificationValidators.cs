using CivisOS.Application.Notifications.DTOs;
using FluentValidation;

namespace CivisOS.Application.Notifications.Validators;

public class RegisterDeviceTokenValidator : AbstractValidator<RegisterDeviceTokenRequest>
{
    public RegisterDeviceTokenValidator()
    {
        RuleFor(x => x.DeviceToken).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Platform).NotEmpty().MaximumLength(50);
    }
}
