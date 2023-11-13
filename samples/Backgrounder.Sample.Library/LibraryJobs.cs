using System;
using System.Threading.Tasks;

namespace Backgrounder.Sample.Library
{
    public class LibraryJobs
    {
        [BackgroundOperation]
        public Task DoWork(int? jobId)
        {
            return Task.FromResult(jobId);
        }

        [BackgroundOperation]
        public Task CompleteWork(int jobId)
        {
            return Task.FromResult(jobId);
        }

        [BackgroundOperation]
        public static Task StaticWork(int jobId)
        {
            return Task.FromResult(jobId);
        }


        [BackgroundOperation(ExtensionName = "RunScheduler")]
        public Task RunSchedule()
        {
            return Task.CompletedTask;
        }

    }
}


namespace System.Runtime.CompilerServices
{
}
