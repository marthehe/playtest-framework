namespace PlayTest.Core.Configuration;

/// <summary>
/// Defines environment-independent settings used by PlayTest clients and fixtures.
/// </summary>
public class PlayTestConfiguration
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public RetryPolicy Retry { get; set; } = new();
}

/// <summary>
/// Defines bounded exponential-backoff settings for retryable test operations.
/// </summary>
public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(200);
    public double BackoffMultiplier { get; set; } = 2.0;
}
