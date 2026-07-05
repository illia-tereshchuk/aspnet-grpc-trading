// A bridge between gRPC and SignalR
// - Updates portfolio state with latest prices from gRPC stream
// - Broadcasts portfolio state snapshot to SignalR hub context

using Grpc.Core;        // For "RpcException" and execution with "cancellationToken"
using Grpc.Net.Client;  // For "GrpcChannel"

using Trading;  // Same as in server. For "TradingFeedClient", "SubscribeRequest", "PriceTick"

using Microsoft.AspNetCore.SignalR;

namespace Portfolio;

public sealed class P_Access : BackgroundService // Registered in "Program.cs", runs in background
{
    // TODO: Can we bring all the parameters to top declaration of class, to omit these repetitions in constructor?
    private readonly P_Store _store;
    private readonly IHubContext<P_Hub> _pHubCtx;
    private readonly IConfiguration _config; // TODO: Explain where it is got from
    private readonly ILogger<P_Access> _logger;

    public P_Access(
        P_Store store,
        IHubContext<P_Hub> pHubCtx,
        IConfiguration config,
        ILogger<P_Access> logger)
    {
        _store = store;
        _pHubCtx = pHubCtx;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) // Entry point for BackgroundService
    {
        var address = ResolveTradingAddress();

        _logger.LogInformation("Connecting to Trading at {Address}", address);

        // Lives for the lifetime of the service, reconnects on the call level.
        using var channel = GrpcChannel.ForAddress(address); // C# 8, declaration using, dispose on method end

        var client = new TradingFeed.TradingFeedClient(channel); // Also generated from .proto, cheaper than channel

        while (!stoppingToken.IsCancellationRequested) // While app is running
        {
            try
            {
                await ReadStreamTicks(client, stoppingToken); // Sit there as long as possible
            }
            // Catch this only if we are NOT in normal shutdown process
            catch (RpcException ex) when (!stoppingToken.IsCancellationRequested) // C# 6 exception filter
            {
                _logger.LogWarning("Stream error ({Status}); retrying in 2s...", ex.StatusCode);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); // Pause before retry
            }
        }
    }

    private async Task ReadStreamTicks(TradingFeed.TradingFeedClient client, CancellationToken ct)
    {
        // Building inner message
        // { _state.Symbols } is "collection initializer", like copy from one to another
        // "SubscribeRequest" generated from .proto
        var request = new SubscribeRequest { Symbols = { _store.Symbols } }; 

        using var call = client.Subscribe(request, cancellationToken: ct);

        // Async streams are new in C# 8.0, .NET Core 3.0+ (and .NET Standard 2.1+).
        await foreach (var tick in call.ResponseStream.ReadAllAsync(ct))
        {
            // Center of all the client logic
            _store.UpdateCurrentPrice(tick.Symbol, tick.Price); 
            await _pHubCtx.Clients.All.SendAsync("snapshot", _store.Snapshot(), ct);
        }
    }

    private string ResolveTradingAddress() => 
        _config["services:trading:http:0"] // Aspire sets it, from "AppHost.cs", by ".WithReference(trading)" (service discovery)
        ?? _config["TRADING_ADDRESS"]
        ?? "http://localhost:5106"; // When ran outside of Aspire, fallback to default address (can use launchsettings.json))
}
