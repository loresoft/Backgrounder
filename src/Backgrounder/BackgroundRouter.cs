using System.Collections.Concurrent;

namespace Backgrounder;

public readonly struct BackgroundRouter
{
    public static ConcurrentDictionary<string, Func<IServiceProvider, byte[], Task>> Processors { get; } = new();

    public static bool Register(string methodSignature, Func<IServiceProvider, byte[], Task> processorAction)
    {
        return Processors.TryAdd(methodSignature, processorAction);
    }
}
