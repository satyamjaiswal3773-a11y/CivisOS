using CivisOS.Application.Permissions.DTOs;
using CivisOS.Domain.Constants;
using FluentValidation;

namespace CivisOS.Application.Permissions.Validators;

public class SetRolePermissionsRequestValidator : AbstractValidator<SetRolePermissionsRequest>
{
    public SetRolePermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionCodes).NotNull();
    }
}

public class SetUserPermissionsRequestValidator : AbstractValidator<SetUserPermissionsRequest>
{
    public SetUserPermissionsRequestValidator()
    {
        RuleFor(x => x.Overrides).NotNull();
        RuleForEach(x => x.Overrides).ChildRules(o =>
        {
            o.RuleFor(x => x.PermissionCode).NotEmpty().MaximumLength(100);
        });
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => AppRoles.All.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Role must be one of: {string.Join(", ", AppRoles.All)}");
    }
}

public class UpdateUserRolesRequestValidator : AbstractValidator<UpdateUserRolesRequest>
{
    public UpdateUserRolesRequestValidator()
    {
        RuleFor(x => x.Roles).NotNull().Must(r => r.Count > 0).WithMessage("At least one role is required.");
        RuleForEach(x => x.Roles)
            .Must(r => AppRoles.All.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Role must be one of: {string.Join(", ", AppRoles.All)}");
    }
}
