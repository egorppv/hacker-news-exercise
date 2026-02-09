using HackerNews.APIClients;
using HackerNewsAPI.Common;
using HackerNewsAPI.Models;
using HackerNewsAPI.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HackerNewsAPI;

public class BackgroundFetcher : BackgroundService
{
    private readonly IHackerNewsClient _hnClient;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<BackgroundFetcher> _logger;
    private readonly HackerNewsOptions _options;
    private readonly Dictionary<int, StoryModel> _loadedStories = new();
    
    private readonly SemaphoreSlim _parallelDownloads;

    public BackgroundFetcher(IHackerNewsClient hnClient, IMemoryCache memoryCache, IOptions<HackerNewsOptions> options, ILogger<BackgroundFetcher> logger)
    {
        _hnClient = hnClient;
        _memoryCache = memoryCache;
        _logger = logger;
        _options = options.Value;
        _parallelDownloads = new SemaphoreSlim(_options.ParallelDownloadsNumber);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundFetcher starting. PollInterval={PollInterval}", _options.PollIntervalTimeSpan);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchAndUpdateCache(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BackgroundFetcher failed to update cache");
            }

            await Task.Delay(_options.PollIntervalTimeSpan, stoppingToken);
        }

        _logger.LogInformation("BackgroundFetcher stopping");
    }

    private async Task FetchAndUpdateCache(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching best stories from {Client}", typeof(IHackerNewsClient));

        var bestStoriesIds = await _hnClient.GetBestStoryIdsAsync(cancellationToken);
        if (bestStoriesIds == null || bestStoriesIds.Count == 0)
        {
            _logger.LogWarning("Received no IDs {Client}", typeof(IHackerNewsClient));
            return;
        }
        _logger.LogInformation("Fetched {ids} best stories from {Client}", bestStoriesIds.Count, typeof(IHackerNewsClient));

        // Only fetch stories that are not already loaded in cache
        var idsToDownload = bestStoriesIds.Except(_loadedStories.Keys).ToArray();
        
        _logger.LogInformation("Fetching stories from {Client}", typeof(IHackerNewsClient));
        var storiesDic = await LoadStories(cancellationToken, idsToDownload);
        
        _logger.LogInformation("Fetched {n} stories from {Client}", storiesDic.Count, typeof(IHackerNewsClient));

        MergeLoadedStoriesToCached(storiesDic, bestStoriesIds);

        // Update cache atomically
        var orderedBestStories = _loadedStories
            .OrderByDescending(i => i.Value.Score)
            .Select(i => i.Value)
            .ToList();
        
        _memoryCache.Set(CommonConstants.CacheKey, orderedBestStories, new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = _options.CacheExpirationTimeSpan
        });

        _logger.LogInformation("Updated cache with {Count} stories", storiesDic.Count);
    }

    private void MergeLoadedStoriesToCached(IDictionary<int, StoryModel> storiesDic, List<int> bestStoriesIds)
    {
        foreach (var storyModelKv in storiesDic)
        {
            if (!_loadedStories.TryAdd(storyModelKv.Key, storyModelKv.Value))
            {
                _logger.LogWarning("Failed to add story {Id} to loaded stories cache. Might be cached", storyModelKv.Key);
            }
        }

        var itemsToDelete = bestStoriesIds.Except(_loadedStories.Keys).ToArray();
        foreach (var i in itemsToDelete)
        {
            if (!_loadedStories.Remove(i))
            {
                _logger.LogWarning("Failed to remove story {Id} from loaded stories cache.", i);
            }
        }
    }

    private async Task<IDictionary<int, StoryModel>> LoadStories(CancellationToken cancellationToken, IEnumerable<int> ids)
    {
        var fetchTasks = ids.Select(id => Task.Run(async () =>
            {
                await _parallelDownloads.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    return await _hnClient.GetStoryAsync(id, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // propagate cancellation
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch item {Id} for {Client}", id, typeof(IHackerNewsClient));
                    return null;
                }
                finally
                {
                    _parallelDownloads.Release();
                }
            }, cancellationToken))
            .ToList();

        var results = await Task.WhenAll(fetchTasks).ConfigureAwait(false);

        var stories = results.Where(s => s != null).ToDictionary(s => s!.Id, es => new StoryModel
        {
            Title = es!.Title,
            Uri = es.Url,
            PostedBy = es.By,
            Time = DateTimeOffset.FromUnixTimeSeconds(es.Time).ToString("o"),
            Score = es.Score ?? 0,
            CommentCount = es.Kids?.Count ?? 0
        });

        return stories;
    }
}
