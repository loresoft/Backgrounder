using Backgrounder.Azure.ServiceBus;
using Backgrounder.Sample.Legacy;
using Backgrounder.Sample.Shared;

namespace Backgrounder.Sample.Worker;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddBackgrounderService();

                services.AddTransient<ISampleJob, SampleJob>();
                services.AddTransient<SampleJob>();
                services.AddTransient<ILibraryJobs, LibraryJobs>();
                services.AddTransient<LibraryJobs>();
            })
            .Build();

        await host.RunAsync();
    }
}
