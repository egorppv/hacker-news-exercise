# HackerNewsAPIClientExample

Simple console app that calls the HackerNewsAPI endpoints.

Usage:

dotnet run --project HackerNewsAPIClientExample -- http://localhost:5000 5
if no argument for TopN stories is provided, the app will use the random 1-100 

First arg: base URL (default http://localhost:5000)
Second arg: n (number of top stories to fetch, default 5)

