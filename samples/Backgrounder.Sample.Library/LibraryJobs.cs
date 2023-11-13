using System;
using System.Threading.Tasks;

namespace Backgrounder.Sample.Library;

public class LibraryJobs
{
    [BackgroundOperation]
    public Task LibraryWork(int? jobId)
    {
        return Task.FromResult(jobId);
    }

    [BackgroundOperation]
    public Task LibraryCompleteWork(int jobId)
    {
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
        return Task.CompletedTask;
    }
}
