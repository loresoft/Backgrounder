namespace Backgrounder.Sample;

public class SampleJob : ISampleJob
{
    private readonly ILogger<Worker> _logger;

    public SampleJob(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    [BackgroundOperation]
    public Task DoWork(int? jobId)
    {
        _logger.LogInformation("DoWork Job {JobId}", jobId);

        return Task.FromResult(jobId);
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


    [BackgroundOperation(ExtensionName = "RunScheduler")]
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
