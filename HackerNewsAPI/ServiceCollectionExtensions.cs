using System.Net;
using HackerNewsAPI.Options;
using HackerNews.APIClients;
using Polly;
using Polly.Extensions.Http;

namespace HackerNewsAPI;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the named HttpClient "hackerNewsClient" and attaches a Polly retry policy
    /// that handles transient HTTP errors, 408/5xx, and 429 (Too Many Requests).
    /// </summary>
    public static IServiceCollection AddHackerNewsHttpClient(this IServiceCollection services, HackerNewsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        services.AddHttpClient<IHackerNewsClient, HackerNewsClient>(client =>
        {
            client.BaseAddress = new Uri(options.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        })
        .AddPolicyHandler((sp, request) =>
        {
            var logger = sp.GetRequiredService<ILogger<BackgroundFetcher>>();

            // Jittered exponential backoff
            return HttpPolicyExtensions
                .HandleTransientHttpError() // HttpRequestException, 5xx and 408
                .OrResult(msg => msg.StatusCode == (HttpStatusCode)429) // also handle 429
                .WaitAndRetryAsync(
                    options.RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        if (outcome.Exception != null)
                        {
                            logger.LogWarning(outcome.Exception, "HackerNews typed client retry {RetryAttempt} after {Delay} due to exception", retryAttempt, timespan);
                        }
                        else
                        {
                            logger.LogWarning("HackerNews typed client retry {RetryAttempt} after {Delay} due to HTTP {StatusCode}", retryAttempt, timespan, (int)outcome.Result!.StatusCode);
                        }
                    });
        });

        return services;
    }
}
