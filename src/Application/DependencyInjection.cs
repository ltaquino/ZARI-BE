namespace ZARI.Application;

using System.Reflection;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Reporting.Datasets;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);
        services.AddHandlersFromAssembly(assembly);
        services.AddReportDatasetsFromAssembly(assembly);

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

    // Report Designer datasets are stateless (all per-request state is passed into RunAsync), so
    // each implementation of IReportDataset is registered once as a singleton against the
    // interface — GetReportDatasetsQueryHandler/RunReportTemplateQueryHandler resolve the full
    // set via IEnumerable<IReportDataset> and look one up by Key.
    private static void AddReportDatasetsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var datasetTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IReportDataset).IsAssignableFrom(t));

        foreach (var type in datasetTypes)
        {
            services.AddSingleton(typeof(IReportDataset), type);
        }
    }
}
