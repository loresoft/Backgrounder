using System.Threading.Tasks;

namespace Backgrounder.Sample.Legacy;
public interface ILibraryJobs
{
    Task LibraryCompleteWork(int jobId);
    Task LibraryWork(int? jobId);
    Task RunSchedule();
}