using Backgrounder.Sample.Shared;

namespace Backgrounder.Sample;

public interface ISampleJob
{
    void CheckPerson(Person person);
    Task CompleteWork(int jobId);
    Task DoWork(int jobId, string? name);
    Task DoWork(int? jobId);
    Task RunSchedule();
    Task WorkError(int? jobId);
}
