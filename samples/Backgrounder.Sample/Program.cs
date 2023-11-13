using System.Runtime.CompilerServices;
using Backgrounder.Sample.Library;

namespace Backgrounder.Sample;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var connectionString = context.Configuration.GetConnectionString("ServiceBusConnectionString");

                services.AddAzureServiceBusBackgrounder(connectionString);
                services.AddHostedService<Worker>();

                services.AddTransient<ISampleJob, SampleJob>();
                services.AddTransient<SampleJob>();
            })
            .Build();

        var backgrounder = host.Services.GetRequiredService<IBackgrounder>();

        await backgrounder.DoWork(123);
        await backgrounder.DoWork(456, "Test");
        await backgrounder.CompleteWork(456);
        await backgrounder.CheckPerson(new Person { Name = "Test" });
        await backgrounder.RunScheduler();

        await backgrounder.LibraryWork(123);

        await host.RunAsync();
    }
}
