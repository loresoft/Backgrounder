using MessagePack;
using MessagePack.Resolvers;

using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backgrounder;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureServiceBusBackgrounder(this IServiceCollection services, string connectionString)
    {
        services
            .AddOptions<BackgrounderOptions>()
            .Configure<IConfiguration>((settings, configuration) => configuration
                .GetSection(BackgrounderOptions.SectionName)
                .Bind(settings)
            );

        services.AddAzureClients(builder =>
        {
            builder.AddServiceBusClient(connectionString);
        });

        services.TryAddSingleton((_) => ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray));
        services.TryAddSingleton<IMessageSerializer, MessagePackMessageSerializer>();

        services.TryAddSingleton<IBackgrounder, ServiceBusBackgrounder>();
        services.TryAddSingleton<IBackgroundService, ServiceBusBackgroundService>();

        services.AddHostedService(sp => sp.GetRequiredService<IBackgroundService>());

        return services;
    }
}
