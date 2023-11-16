using MessagePack;
using MessagePack.Resolvers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;

namespace Backgrounder.RabbitMQ;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackgrounder(this IServiceCollection services, string connectionString, string queueName)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or empty.", nameof(connectionString));

        if (string.IsNullOrEmpty(queueName))
            throw new ArgumentException($"'{nameof(queueName)}' cannot be null or empty.", nameof(queueName));

        return services.AddBackgrounder(options =>
        {
            options.ConnectionString = connectionString;
            options.QueueName = queueName;
        });
    }

    public static IServiceCollection AddBackgrounder(this IServiceCollection services, Action<BackgrounderOptions>? configureOptions = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services
            .AddOptions<BackgrounderOptions>()
            .Configure<IConfiguration>((settings, configuration) => configuration
                .GetSection(BackgrounderOptions.SectionName)
                .Bind(settings)
            );

        if (configureOptions != null)
            services.Configure(configureOptions);

        services.TryAddSingleton<IConnectionFactory>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BackgrounderOptions>>();
            var connectionString = options.Value.ConnectionString;

            var factory = new ConnectionFactory
            {
                Uri = new(connectionString),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            return factory;
        });
        services.TryAddSingleton<IConnection>(sp => sp.GetRequiredService<IConnectionFactory>().CreateConnection());

        services.TryAddSingleton((_) => ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray));
        services.TryAddSingleton<IMessageSerializer, MessagePackMessageSerializer>();

        services.TryAddSingleton<IBackgrounder, RabbitBackgrounder>();
        services.TryAddSingleton<IBackgroundService, RabbitBackgroundService>();
        services.TryAddSingleton<RabbitBackgroundService>();

        return services;
    }


    public static IServiceCollection AddBackgrounderService(this IServiceCollection services, string connectionString, string queueName)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or empty.", nameof(connectionString));

        if (string.IsNullOrEmpty(queueName))
            throw new ArgumentException($"'{nameof(queueName)}' cannot be null or empty.", nameof(queueName));

        return services.AddBackgrounderService(options =>
        {
            options.ConnectionString = connectionString;
            options.QueueName = queueName;
        });
    }

    public static IServiceCollection AddBackgrounderService(this IServiceCollection services, Action<BackgrounderOptions>? configureOptions = null)
    {
        services.AddBackgrounder(configureOptions);

        services.AddHostedService(sp => sp.GetRequiredService<RabbitBackgroundService>());

        return services;
    }
}
