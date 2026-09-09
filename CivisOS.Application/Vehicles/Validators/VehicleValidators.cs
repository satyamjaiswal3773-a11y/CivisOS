using CivisOS.Application.Vehicles.DTOs;
using CivisOS.Domain.Enums;
using FluentValidation;

namespace CivisOS.Application.Vehicles.Validators;

public class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Make).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.VehicleTypeId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.FuelCapacityLiters).GreaterThan(0).When(x => x.FuelCapacityLiters.HasValue);
        RuleFor(x => x.CurrentFuelLevelLiters).GreaterThanOrEqualTo(0).When(x => x.CurrentFuelLevelLiters.HasValue);
        RuleFor(x => x.OdometerKm).GreaterThanOrEqualTo(0).When(x => x.OdometerKm.HasValue);
    }
}

public class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleRequestValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Make).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.VehicleTypeId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.FuelCapacityLiters).GreaterThan(0).When(x => x.FuelCapacityLiters.HasValue);
        RuleFor(x => x.CurrentFuelLevelLiters).GreaterThanOrEqualTo(0).When(x => x.CurrentFuelLevelLiters.HasValue);
        RuleFor(x => x.OdometerKm).GreaterThanOrEqualTo(0).When(x => x.OdometerKm.HasValue);
    }
}

public class CreateVehicleDocumentRequestValidator : AbstractValidator<CreateVehicleDocumentRequest>
{
    public CreateVehicleDocumentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.DocumentNumber).MaximumLength(100);
    }
}

public class CreateVehicleTypeRequestValidator : AbstractValidator<CreateVehicleTypeRequest>
{
    public CreateVehicleTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
