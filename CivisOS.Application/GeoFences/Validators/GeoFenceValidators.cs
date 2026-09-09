using CivisOS.Application.GeoFences.DTOs;
using FluentValidation;

namespace CivisOS.Application.GeoFences.Validators;

public class CreateGeoFenceRequestValidator : AbstractValidator<CreateGeoFenceRequest>
{
    public CreateGeoFenceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.CenterLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.CenterLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusMeters).GreaterThan(0).LessThanOrEqualTo(50_000);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class UpdateGeoFenceRequestValidator : AbstractValidator<UpdateGeoFenceRequest>
{
    public UpdateGeoFenceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.CenterLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.CenterLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.RadiusMeters).GreaterThan(0).LessThanOrEqualTo(50_000);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class GeoFenceCheckRequestValidator : AbstractValidator<GeoFenceCheckRequest>
{
    public GeoFenceCheckRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.GeoFenceId).NotEmpty();
    }
}
