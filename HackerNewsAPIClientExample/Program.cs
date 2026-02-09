using System.Net.Http.Json;

var host = args.Length > 0 ? args[0] : "http://localhost:5000";
var n = args.Length > 1 && int.TryParse(args[1], out var parsed) ? parsed : Random.Shared.Next(1,101);

using var client = new HttpClient { BaseAddress = new Uri(host) };

Console.WriteLine("Press any key to stop the loop at any time.");

// Use a CancellationTokenSource which will be canceled when any key is pressed
using var cts = new CancellationTokenSource();
_ = Task.Run(() => { Console.ReadKey(true); cts.Cancel(); });

int iteration = 0;

while (true)
{
    if (cts.IsCancellationRequested)
    {
        Console.WriteLine("Key pressed — stopping.");
        break;
    }

    iteration++;
    Console.WriteLine($"[{DateTime.Now:O}] Fetching top {n} stories (iteration {iteration})...");

    try
    {
        var stories = await client.GetFromJsonAsync<List<object>>($"/api/BestStories/{n}");
        Console.WriteLine($"Fetched {stories?.Count ?? 0} stories:");
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(stories, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Fetch failed: {ex.Message}");
    }

    // Randomized delay between 5 and 30 seconds
    var delaySeconds = Random.Shared.Next(5, 31);
    Console.WriteLine($"Waiting {delaySeconds} seconds before next request. Press any key to stop.");

    try
    {
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cts.Token);
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("Key pressed — stopping before next iteration.");
        break;
    }
}

Console.WriteLine("Done.");

