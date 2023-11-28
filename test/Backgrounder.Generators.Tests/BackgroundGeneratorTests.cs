using System.Collections.Immutable;

using FluentAssertions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Backgrounder.Generators.Tests;

[UsesVerify]
public class BackgroundGeneratorTests
{
    [Fact]
    public Task GenerateSingleParameter()
    {
        var source = @"
using Backgrounder;

namespace Backgrounder.Sample;

public interface ISampleJob
{
    Task DoWork(int? jobId);
}

public class SampleJob : ISampleJob
{
    [BackgroundOperation]
    public Task DoWork(int? jobId) => Task.FromResult(jobId);
}
";

        var (diagnostics, output) = GetGeneratedOutput<BackgroundGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateMultipleParameter()
    {
        var source = @"
using Backgrounder;

namespace Backgrounder.Sample;

public interface ISampleJob
{
    Task DoWork(int jobId, string? name);
}

public class SampleJob : ISampleJob
{
    [BackgroundOperation<ISampleJob>]
    public Task DoWork(int jobId, string? name) => Task.FromResult(jobId);
}
";

        var (diagnostics, output) = GetGeneratedOutput<BackgroundGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateStaticMethod()
    {
        var source = @"
using Backgrounder;

namespace Backgrounder.Sample;

public class SampleJob
{
    [BackgroundOperation]
    public static Task StaticWork(int jobId)
    {
        return Task.FromResult(jobId);
    }
}
";

        var (diagnostics, output) = GetGeneratedOutput<BackgroundGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateComplexParameter()
    {
        var source = @"
using Backgrounder;

namespace Backgrounder.Sample;

public interface ISampleJob
{
    void CheckPerson(Person person);
}

public class SampleJob : ISampleJob
{
    [BackgroundOperation]
    public void CheckPerson(Person person) { }
}

public class Person
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}
";

        var (diagnostics, output) = GetGeneratedOutput<BackgroundGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateAttributeName()
    {
        var source = @"
using Backgrounder;

namespace Backgrounder.Sample;

public interface ISampleJob
{
    Task RunSchedule();
}

public class SampleJob : ISampleJob
{
    [BackgroundOperation(ExtensionName = ""RunScheduler"", ServiceType = typeof(ISampleJob))]
    public Task RunSchedule() => Task.CompletedTask;
}
";

        var (diagnostics, output) = GetGeneratedOutput<BackgroundGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, string Output) GetGeneratedOutput<T>(string source)
        where T : IIncrementalGenerator, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(T).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(BackgroundOperationAttribute).Assembly.Location),
            });

        var compilation = CSharpCompilation.Create(
            "generator",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var originalTreeCount = compilation.SyntaxTrees.Length;
        var generator = new T();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var trees = outputCompilation.SyntaxTrees.ToList();

        var output = trees.Count != originalTreeCount ? trees[^1].ToString() : string.Empty;

        return (diagnostics, output);
    }
}
