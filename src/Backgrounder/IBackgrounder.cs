using System.Runtime.CompilerServices;

namespace Backgrounder;

public interface IBackgrounder
{
    Task EnqueueAsync<TParameters>(string methodSignature, TParameters? methodParameters);
}
