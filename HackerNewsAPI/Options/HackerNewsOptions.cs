namespace HackerNewsAPI.Options;

public class HackerNewsOptions
{
    public string ApiBaseUrl { get; set; } = "https://hacker-news.firebaseio.com/v0/";
    public TimeSpan PollIntervalTimeSpan { get; set; } = System.TimeSpan.FromSeconds(60);
    public int RequestTimeoutSeconds { get; set; } = 10;
    public int RetryCount { get; set; } = 3;
    public int ParallelDownloadsNumber { get; set; } = 10;
    public TimeSpan CacheExpirationTimeSpan { get; set; } = System.TimeSpan.FromDays(1);
}
