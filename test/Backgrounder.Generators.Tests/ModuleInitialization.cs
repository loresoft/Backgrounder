using System.Runtime.CompilerServices;

namespace Backgrounder.Generators.Tests;

public static class ModuleInitialization
{
    [ModuleInitializer]
    public static void Initialize() => VerifySourceGenerators.Initialize();
}
