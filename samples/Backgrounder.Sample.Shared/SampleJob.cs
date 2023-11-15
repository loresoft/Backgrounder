using Microsoft.Extensions.Logging;

namespace Backgrounder.Sample.Shared;

public class SampleJob : ISampleJob
{
    private readonly ILogger<SampleJob> _logger;

    public SampleJob(ILogger<SampleJob> logger)
    {
        _logger = logger;
    }

    [BackgroundOperation]
    public Task DoWork(int? jobId)
    {
        _logger.LogInformation("DoWork Job {JobId}", jobId);

        return Task.FromResult(jobId);
    }

    [BackgroundOperation]
    public Task WorkError(int? jobId)
    {
        _logger.LogInformation("WorkError Job {JobId}", jobId);

        var exception = new InvalidOperationException("Work Error");

        return Task.FromException(exception);
    }

    [BackgroundOperation<ISampleJob>]
    public Task DoWork(int jobId, string? name)
    {
        _logger.LogInformation("DoWork Job {JobId}, Name {name}", jobId, name);

        return Task.FromResult(jobId);
    }

    [BackgroundOperation]
    public Task CompleteWork(int jobId)
    {
        _logger.LogInformation("CompleteWork Job {JobId}", jobId);

        return Task.FromResult(jobId);
    }

    [BackgroundOperation]
    public void CheckPerson(Person person)
    {
        _logger.LogInformation("CheckPerson Name {name}", person.Name);

    }

    [BackgroundOperation]
    public static Task StaticWork(int jobId)
    {
        return Task.FromResult(jobId);
    }


    [BackgroundOperation(ExtensionName = "RunScheduler", ServiceType = typeof(ISampleJob))]
    public Task RunSchedule()
    {
        _logger.LogInformation("RunSchedule()");

        return Task.CompletedTask;
    }
}

public class Person
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}
