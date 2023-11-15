using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgrounder.Azure.ServiceBus;

public class ServiceBusBackgroundService : IBackgroundService
{
    private readonly ILogger<ServiceBusBackgroundService> _logger;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IOptions<BackgrounderOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    private readonly Lazy<ServiceBusProcessor> _serviceBusProcessor;
    private readonly Lazy<ServiceBusSender> _serviceBusSender;

    private readonly ThreadLocal<Random> _random = new(() => new Random());

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

    public bool IsBusy => _activeProcesses > 0;

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

        // wait for processing to finish
        var timeout = DateTimeOffset.UtcNow.AddMinutes(5);
        while (IsBusy && timeout > DateTimeOffset.UtcNow)
        {
            await Task.Delay(500, cancellationToken);
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


            var scheduledMessage = new ServiceBusMessage(processMessage.Message);

            var (delay, retryCount) = RetryDelay(scheduledMessage);

            if (retryCount <= _options.Value.MaxRetryAttempts)
            {
                await _serviceBusSender.Value.ScheduleMessageAsync(scheduledMessage, DateTimeOffset.UtcNow.Add(delay));

                // complete original
                await processMessage.CompleteMessageAsync(processMessage.Message);
            }
            else
            {
                await processMessage.DeadLetterMessageAsync(processMessage.Message, "Max message retry reached", "The maximum message retry has been reached");
            }
        }
        finally
        {
            Interlocked.Decrement(ref _activeProcesses);
        }
    }

    private (TimeSpan, int) RetryDelay(ServiceBusMessage scheduledMessage)
    {
        scheduledMessage.ApplicationProperties.TryGetValue("RetryCount", out var retryValue);
        if (retryValue is int retryCount)
            retryCount++;
        else
            retryCount = 1;

        scheduledMessage.ApplicationProperties.TryGetValue("DelayState", out var delayValue);
        if (delayValue is not double delayState)
            delayState = 0;

        var options = _options.Value;
        var backoffDelay = RetryHelper.GetRetryDelay(
            type: options.BackoffType,
            jitter: options.UseJitter,
            attempt: retryCount,
            baseDelay: options.RetryDelay,
            maxDelay: options.MaxRetryDelay,
            state: ref delayState,
            randomizer: _random.Value!.NextDouble);

        scheduledMessage.ApplicationProperties["RetryCount"] = retryCount;
        scheduledMessage.ApplicationProperties["DelayState"] = delayState;

        return (backoffDelay, retryCount);
    }
}
