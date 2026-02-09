namespace HackerNews.APIClients.Models;

public record StoryItemModel(
    int Id,
    string? By,
    long Time,
    string? Title,
    string? Url,
    string? Type,
    int? Score,
    List<int>? Kids);

