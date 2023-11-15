namespace Backgrounder;

public class BackgrounderOptions
{
    public const string SectionName = "Backgrounder";

    public string QueueName { get; set; } = null!;

    public string ConnectionString { get; set; } = null!;

    public BackoffType BackoffType { get; set; } = BackoffType.Linear;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    public TimeSpan? MaxRetryDelay { get; set; }

    public int MaxRetryAttempts { get; set; } = 10;

    public bool UseJitter { get; set; } = true;

}
