using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CivisOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
