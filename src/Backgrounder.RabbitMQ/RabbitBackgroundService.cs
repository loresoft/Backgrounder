using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Backgrounder.RabbitMQ;

public class RabbitBackgroundService : IBackgroundService, IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
