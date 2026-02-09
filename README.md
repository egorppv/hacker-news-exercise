The repository contains the implementation for the Best Hacker News API assignment.

It includes two executable console applications:
- HackerNewsAPI - the hosting process responsible for exposing the API.
- HackerNewsAPIClientExample - a client application used to send requests to the API. Multiple instances of this application can be executed concurrently.

Additionally, the repository contains HackerNewsAPIClients, which provides client implementations for parsing Hacker News and potentially other external sources.

To launch the example:
1. Open the solution.
2. Launch HackerNewsAPI project
3. Launch HackerNewsAPIClientExample project and it will connect to the API on 5000 port


The HackerNewsAPI application is composed of two main components:
- A BackgroundService that periodically polls the target website at a configured interval.
- BestStoriesController, which exposes the API endpoint.

The BackgroundService includes several optimizations:
- Parallel fetching of new stories with a configurable limit on the maximum number of concurrent requests.
- Caching of previously fetched stories to avoid redundant network calls.
- A policy for downloading data from the website is implemented using the Polly library, enabling configuration to prevent overloading the site.
- Sorting of stories during the fetch process and persisting them in an already sorted state.
Within BestStoriesController, the data is served directly from the cache in a preprocessed form. This allows clients to retrieve the requested number of items with minimal latency and ensures efficient operation under high levels of concurrent access.

Concerns:
- Downloaded items are stored twice: in the download cache and in the result cache. This can be optimized by using a single cache that stores stories in an ordered form, simplifying both reading and storage.
- A HealthCheck endpoint could provide information about potential download issues.
- There is a delay based on the configured UpdateInterval for the background job, meaning the data is not real-time but is optimized for high-intensity read scenarios.

