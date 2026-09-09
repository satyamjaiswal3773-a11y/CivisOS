using CivisOS.Application.Employees.DTOs;
using CivisOS.Domain.Enums;
using FluentValidation;

namespace CivisOS.Application.Employees.Validators;

public class CreateDepartmentRequestValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CreateDesignationRequestValidator : AbstractValidator<CreateDesignationRequest>
{
    public CreateDesignationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.JoiningDate).NotEmpty();
        RuleFor(x => x.Shift).IsInEnum();
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.DesignationId).NotEmpty();
        RuleFor(x => x.UserId).MaximumLength(450).When(x => !string.IsNullOrWhiteSpace(x.UserId));
    }
}

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.JoiningDate).NotEmpty();
        RuleFor(x => x.Shift).IsInEnum();
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.DesignationId).NotEmpty();
        RuleFor(x => x.UserId).MaximumLength(450).When(x => !string.IsNullOrWhiteSpace(x.UserId));
    }
}

public class AssignSupervisorRequestValidator : AbstractValidator<AssignSupervisorRequest>
{
    public AssignSupervisorRequestValidator()
    {
        // SupervisorId may be null to clear assignment
    }
}

public class AssignVehicleDriverRequestValidator : AbstractValidator<AssignVehicleDriverRequest>
{
    public AssignVehicleDriverRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}
