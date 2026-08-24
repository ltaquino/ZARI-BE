namespace ZARI.Application;

using System.Reflection;
using ZARI.Application.Abstractions.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);
        services.AddHandlersFromAssembly(assembly);

        return services;
    }

    private static void AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var handlerInterfaceTypes = new[]
        {
            typeof(ICommandHandler<,>),
            typeof(IQueryHandler<,>)
        };

        var types = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .ToList();

        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            handlerInterfaceTypes.Contains(i.GetGenericTypeDefinition()));

            foreach (var handlerInterface in interfaces)
            {
                services.AddScoped(handlerInterface, type);
            }
        }
    }
}
