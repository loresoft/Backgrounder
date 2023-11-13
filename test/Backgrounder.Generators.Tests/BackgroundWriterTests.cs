namespace Backgrounder.Generators.Tests;

[UsesVerify]
public class BackgroundWriterTests
{
    [Fact]
    public Task GenerateWithOneParameter()
    {
        var backgroundMethod = new BackgroundMethod(
            ClassNamespace: "Backgrounder.Sample",
            ClassName: "SampleJob",
            ServiceType: "SampleJob",
            MethodName: "DoWork",
            ExtensionName: "DoWork",
            MethodSignature: "public Task DoWork(int?)",
            MethodHash: "ABCD",
            IsStatic: false,
            IsAsync: true,
            Parameters: new Internal.EquatableArray<MethodParameter>(new[]
            {
                new MethodParameter("jobId", "int?")
            })
        );


        var source = BackgroundWriter.Generate(backgroundMethod);

        return Verifier
            .Verify(source)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }


    [Fact]
    public Task GenerateWithMultipleParameter()
    {
        var backgroundMethod = new BackgroundMethod(
            ClassNamespace: "Backgrounder.Sample",
            ClassName: "SampleJob",
            ServiceType: "ISampleJob",
            MethodName: "MultipleParameter",
            ExtensionName: "MultipleParameter",
            MethodSignature: "public Task DoWork(int?, string?)",
            MethodHash: "XEWDF",
            IsStatic: false,
            IsAsync: true,
            Parameters: new Internal.EquatableArray<MethodParameter>(new[]
            {
                new MethodParameter("jobId", "int?"),
                new MethodParameter("name", "string?"),
            })
        );


        var source = BackgroundWriter.Generate(backgroundMethod);

        return Verifier
            .Verify(source)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }
}
