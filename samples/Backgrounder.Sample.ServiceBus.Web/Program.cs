
using Backgrounder.Azure.ServiceBus;
using Backgrounder.Sample.Legacy;
using Backgrounder.Sample.Shared;

namespace Backgrounder.Sample.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthorization();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddBackgrounder();

        builder.Services.AddTransient<ISampleJob, SampleJob>();
        builder.Services.AddTransient<SampleJob>();
        builder.Services.AddTransient<ILibraryJobs, LibraryJobs>();
        builder.Services.AddTransient<LibraryJobs>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        MapRoutes(app);

        app.Run();
    }

    private static void MapRoutes(WebApplication application)
    {
        application
            .MapGet("/sample/do-work/{jobId}", async (IBackgrounder backgrounder, int jobId) =>
            {
                await backgrounder.DoWork(jobId);
                Results.Ok();
            })
            .WithName("SampleDoWork")
            .WithOpenApi();

        application
            .MapGet("/sample/do-work/{jobId}/{name}", async (IBackgrounder backgrounder, int jobId, string name) =>
            {
                await backgrounder.DoWork(jobId, name);
                Results.Ok();
            })
            .WithName("SampleDoWorkName")
            .WithOpenApi();

        application
            .MapGet("/sample/complete-work/{jobId}", async (IBackgrounder backgrounder, int jobId) =>
            {
                await backgrounder.CompleteWork(jobId);
                Results.Ok();
            })
            .WithName("SampleCompleteWork")
            .WithOpenApi();

        application
            .MapPost("/sample/check-person", async (IBackgrounder backgrounder, Person person) =>
            {
                await backgrounder.CheckPerson(person);
                Results.Ok();
            })
            .WithName("SampleCheckPerson")
            .WithOpenApi();

        application
            .MapGet("/sample/run-scheduler", async (IBackgrounder backgrounder) =>
            {
                await backgrounder.RunScheduler();
                Results.Ok();
            })
            .WithName("SampleRunScheduler")
            .WithOpenApi();

        application
            .MapGet("/legacy/library-work/{jobId}", async (IBackgrounder backgrounder, int jobId) =>
            {
                await backgrounder.LibraryWork(jobId);
                Results.Ok();
            })
            .WithName("LegacyLibraryWork")
            .WithOpenApi();

        application
            .MapGet("/legacy/library-complete-work/{jobId}", async (IBackgrounder backgrounder, int jobId) =>
            {
                await backgrounder.LibraryCompleteWork(jobId);
                Results.Ok();
            })
            .WithName("LegacyCompleteWork")
            .WithOpenApi();

    }
}
