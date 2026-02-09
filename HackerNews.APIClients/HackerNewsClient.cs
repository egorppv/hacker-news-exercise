using System.Net.Http.Json;
using HackerNews.APIClients.Models;
using Microsoft.Extensions.Logging;

namespace HackerNews.APIClients;

public interface IHackerNewsClient
{
    Task<List<int>?> GetBestStoryIdsAsync(CancellationToken cancellationToken);
    Task<StoryItemModel?> GetStoryAsync(int id, CancellationToken cancellationToken);
}

public class HackerNewsClient : IHackerNewsClient
{
    private const string BestStoriesPath = "beststories.json";
    private const string ItemPathFormat = "item/{0}.json";

    private readonly HttpClient _httpClient;
    private readonly ILogger<HackerNewsClient> _logger;

    public HackerNewsClient(HttpClient httpClient, ILogger<HackerNewsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<int>?> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<int>>(BestStoriesPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch best story IDs");
            return null;
        }
    }

    public async Task<StoryItemModel?> GetStoryAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _httpClient.GetFromJsonAsync<StoryItemModel?>(string.Format(ItemPathFormat, id), cancellationToken);
            if (item == null || item.Type != "story") return null;

            return item;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch item {Id}", id);
            return null;
        }
    }
}