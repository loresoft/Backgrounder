using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgrounder;

public class ServiceBusBackgroundService : IBackgroundService
{
    private readonly ILogger<ServiceBusBackgroundService> _logger;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IOptions<BackgrounderOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    private readonly Lazy<ServiceBusProcessor> _serviceBusProcessor;
    private readonly Lazy<ServiceBusSender> _serviceBusSender;

    private int _activeProcesses;

    public ServiceBusBackgroundService(
        ILogger<ServiceBusBackgroundService> logger,
        IServiceProvider serviceProvider,
        ServiceBusClient serviceBusClient,
        IOptions<BackgrounderOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _serviceBusSender = new Lazy<ServiceBusSender>(() => _serviceBusClient.CreateSender(_options.Value.QueueName));
        _serviceBusProcessor = new Lazy<ServiceBusProcessor>(() => _serviceBusClient.CreateProcessor(_options.Value.QueueName));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _serviceBusProcessor.Value.ProcessMessageAsync += MessageHandler;
        _serviceBusProcessor.Value.ProcessErrorAsync += ErrorHandler;

        await _serviceBusProcessor.Value.StartProcessingAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_serviceBusProcessor.IsValueCreated)
        {
            _serviceBusProcessor.Value.ProcessMessageAsync -= MessageHandler;
            _serviceBusProcessor.Value.ProcessErrorAsync -= ErrorHandler;

            await _serviceBusProcessor.Value.StopProcessingAsync(cancellationToken);
        }
    }


    private Task ErrorHandler(ProcessErrorEventArgs processMessage)
    {
        var ex = processMessage.Exception;
        _logger.LogError(ex, "Error processing background work: {message}", ex.Message);

        return Task.CompletedTask;
    }

    private async Task MessageHandler(ProcessMessageEventArgs processMessage)
    {
        var message = processMessage.Message;

        using var scope = _logger.BeginScope("Identifier: {Identifier}; Message: {MessageId}; Subject: {Subject}", processMessage.Identifier, message.MessageId, message.Subject);
        try
        {
            Interlocked.Increment(ref _activeProcesses);

            var methodSignature = message.Subject;

            _logger.LogInformation("Process background work for: {methodSignature}", methodSignature);

            BackgroundRouter.Processors.TryGetValue(methodSignature, out var processor);

            if (processor == null)
            {
                _logger.LogError("Could not find background processor for '{methodSignature}'", methodSignature);
                await processMessage.DeadLetterMessageAsync(message, "Processor not found", $"Could not find background processor for '{methodSignature}'");
                return;
            }

            await processor(_serviceProvider, message.Body.ToArray());
            await processMessage.CompleteMessageAsync(processMessage.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing background work: {message}", ex.Message);

            // schedule retry
            var scheduledMessage = new ServiceBusMessage(processMessage.Message);
            await _serviceBusSender.Value.ScheduleMessageAsync(scheduledMessage, DateTimeOffset.UtcNow.AddMinutes(5));

            // complete orginal
            await processMessage.CompleteMessageAsync(processMessage.Message);
        }
        finally
        {
            Interlocked.Decrement(ref _activeProcesses);
        }
    }


}
