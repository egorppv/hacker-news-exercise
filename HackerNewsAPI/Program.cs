using System.Text.Json.Serialization;
using HackerNewsAPI.Options;
using HackerNewsAPI;
using Serilog;

// Create a simple Serilog console logger before building the host
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting application");

    var builder = WebApplication.CreateBuilder(args);

    // Attach Serilog to the generic host
    builder.Host.UseSerilog();

    // Read Kestrel hosting port from configuration (optional)
    // Config path: "Hosting:Kestrel:HttpPort" (int). Falls back to 5000.
    var kestrelPort = builder.Configuration.GetValue<int?>("Hosting:Kestrel:HttpPort")
                      ?? builder.Configuration.GetValue<int?>("Kestrel:HttpPort")
                      ?? 5000;

    // Configure Kestrel to listen on the configured port
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureEndpointDefaults(listenOptions => { /* keep defaults */ });
        options.ListenAnyIP(kestrelPort);
    });

    // Configuration defaults and binding
    builder.Services.Configure<HackerNewsOptions>(builder.Configuration.GetSection("BackgroundFetcher") );
    var hnOptions = new HackerNewsOptions();
    builder.Configuration.GetSection("BackgroundFetcher").Bind(hnOptions);

    builder.Services.AddMemoryCache();

    // Use extension method to register client and policies
    builder.Services.AddHackerNewsHttpClient(hnOptions);

    // Register hosted background service from this project
    builder.Services.AddHostedService<BackgroundFetcher>();

    builder.Services.AddControllers().AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

    var app = builder.Build();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
