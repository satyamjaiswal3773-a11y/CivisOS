using CivisOS.Application.Attendances.DTOs;
using FluentValidation;

namespace CivisOS.Application.Attendances.Validators;

public class AttendanceCheckInRequestValidator : AbstractValidator<AttendanceCheckInRequest>
{
    public AttendanceCheckInRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.GeoFenceId).NotEmpty();
    }
}

public class AttendanceCheckOutRequestValidator : AbstractValidator<AttendanceCheckOutRequest>
{
    public AttendanceCheckOutRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
