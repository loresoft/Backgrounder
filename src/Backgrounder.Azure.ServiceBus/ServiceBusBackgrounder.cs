using System.Runtime.CompilerServices;

using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgrounder;

public class ServiceBusBackgrounder : IBackgrounder
{
    private readonly ILogger<ServiceBusBackgrounder> _logger;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IOptions<BackgrounderOptions> _backgrounderOptions;
    private readonly IMessageSerializer _messageSerializer;

    private readonly Lazy<ServiceBusSender> _serviceBusSender;

    public ServiceBusBackgrounder(
        ILogger<ServiceBusBackgrounder> logger,
        ServiceBusClient serviceBusClient,
        IOptions<BackgrounderOptions> options,
        IMessageSerializer messageSerializer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        _backgrounderOptions = options ?? throw new ArgumentNullException(nameof(options));
        _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));

        _serviceBusSender = new Lazy<ServiceBusSender>(() => _serviceBusClient.CreateSender(_backgrounderOptions.Value.QueueName));
    }

    public async Task EnqueueAsync<TParameters>(string methodSignature, TParameters? methodParameters)
    {
        if (string.IsNullOrWhiteSpace(methodSignature))
            throw new ArgumentException($"'{nameof(methodSignature)}' cannot be null or whitespace.", nameof(methodSignature));

        _logger.LogInformation("Enqueue background work for: {methodSignature}", methodSignature);

        var messageBody = methodParameters != null
            ? _messageSerializer.Serialize(methodParameters)
            : Array.Empty<byte>();

        var message = new ServiceBusMessage(messageBody);
        message.ContentType = "application/msgpack";
        message.Subject = methodSignature;

        await _serviceBusSender.Value.SendMessageAsync(message);
    }
}
