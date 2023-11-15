namespace Backgrounder;

public class BackgrounderOptions
{
    public const string SectionName = "Backgrounder";

    public string QueueName { get; set; } = null!;

    public string ConnectionString { get; set; } = null!;
}
