using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Backgrounder.Sample.Legacy;

public class LibraryJobs : ILibraryJobs
{
    private readonly ILogger<LibraryJobs> _logger;

    public LibraryJobs(ILogger<LibraryJobs> logger)
    {
        _logger = logger;
    }

    [BackgroundOperation]
    public Task LibraryWork(int? jobId)
    {
        _logger.LogInformation("Library Work {JobId}", jobId);
        return Task.FromResult(jobId);
    }

    [BackgroundOperation(ServiceType = typeof(ILibraryJobs))]
    public Task LibraryCompleteWork(int jobId)
    {
        _logger.LogInformation("Library Complete Work Job {JobId}", jobId);
        return Task.FromResult(jobId);
    }

    [BackgroundOperation]
    public static Task LibraryStaticWork(int jobId)
    {
        return Task.FromResult(jobId);
    }


    [BackgroundOperation(ExtensionName = "LibraryScheduler")]
    public Task RunSchedule()
    {
        _logger.LogInformation("Library Run Schedule");
        return Task.CompletedTask;
    }
}
