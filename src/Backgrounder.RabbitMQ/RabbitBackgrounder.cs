using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;

namespace Backgrounder.RabbitMQ;

public class RabbitBackgrounder : IBackgrounder
{
    private readonly ILogger<RabbitBackgrounder> _logger;
    private readonly IConnection _connection;
    private readonly IOptions<BackgrounderOptions> _backgrounderOptions;
    private readonly IMessageSerializer _messageSerializer;

    private readonly Lazy<IModel> _channel;

    public RabbitBackgrounder(
        ILogger<RabbitBackgrounder> logger,
        IConnection connection,
        IOptions<BackgrounderOptions> backgrounderOptions,
        IMessageSerializer messageSerializer)
    {
        _logger = logger;
        _connection = connection;
        _backgrounderOptions = backgrounderOptions;
        _messageSerializer = messageSerializer;

        _channel = new Lazy<IModel>(() =>
        {
            var channel = _connection.CreateModel();
            channel.QueueDeclare(_backgrounderOptions.Value.QueueName, true, false, false, null);

            return channel;
        });
    }
    
    public Task EnqueueAsync<TParameters>(string methodSignature, TParameters? methodParameters)
    {
        if (string.IsNullOrWhiteSpace(methodSignature))
            throw new ArgumentException($"'{nameof(methodSignature)}' cannot be null or whitespace.", nameof(methodSignature));

        _logger.LogInformation("Enqueue background work for: {methodSignature}", methodSignature);

        var messageBody = methodParameters != null
            ? _messageSerializer.Serialize(methodParameters)
            : Array.Empty<byte>();

        var properties = _channel.Value.CreateBasicProperties();
        properties.ContentType = "application/msgpack";
        properties.Persistent = true;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.Type = methodSignature;

        _channel.Value.BasicPublish(
            exchange: string.Empty,
            routingKey: _backgrounderOptions.Value.QueueName,
            basicProperties: properties,
            body: messageBody);

        return Task.CompletedTask;
    }
}
