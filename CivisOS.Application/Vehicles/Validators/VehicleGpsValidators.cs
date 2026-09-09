using CivisOS.Application.Vehicles.DTOs;
using FluentValidation;

namespace CivisOS.Application.Vehicles.Validators;

public class UpdateVehicleLocationRequestValidator : AbstractValidator<UpdateVehicleLocationRequest>
{
    public UpdateVehicleLocationRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.SpeedKmh)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(400)
            .When(x => x.SpeedKmh.HasValue);
        RuleFor(x => x.Timestamp)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .When(x => x.Timestamp.HasValue)
            .WithMessage("Timestamp cannot be more than 5 minutes in the future.");
    }
}
