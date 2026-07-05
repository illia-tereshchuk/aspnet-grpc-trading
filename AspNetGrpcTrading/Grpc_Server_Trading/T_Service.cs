using Grpc.Core;

namespace Trading; // File-scoped namespace (C# 10, 2021). Same namespace as generated .proto types.

// gRPC streaming feed 
// Holds current price for each known symbol
// Random-walks in small ±N% steps on each tick
public sealed class T_Service : TradingFeed.TradingFeedBase // Base class generated from .proto
{
    private static readonly IReadOnlyDictionary<string, double> SeedPrices = new Dictionary<string, double>
    {
        ["DOGE"] = 142.0, // Indexer initializer (C# 6, 2015)
        ["HRLD"] = 175.0,
        ["SDFG"] = 133.0,
        ["FNDG"] = 190.0,
        ["MNKY"] = 120.0,
        ["KRMT"] = 115.0,
        ["WMNY"] = 145.0,
    };

    private const double DefaultSeedPrice = 100.0; // For the case of unknown symbol
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(1000);
    private readonly ILogger<T_Service> _logger;
    public T_Service(ILogger<T_Service> logger) => _logger = logger; // Expression-bodied ctor (C# 6/7)

    public override async Task Subscribe(
        SubscribeRequest request, // From .proto, with "Symbols" string array
        IServerStreamWriter<PriceTick> responseStream, // gRPC: where to write (Grpc.Core), "PriceTick" from .proto
        ServerCallContext context) // Grpc.Core, contains cancellation token
    {
        var symbols = request.Symbols.Count > 0 // If the client sent symbols, use them
            ? request.Symbols.Distinct().ToArray()
            : SeedPrices.Keys.ToArray(); // Otherwise - subscribe to all known symbols

        var basePrices = symbols.ToDictionary( // These we will change to ±35%
            s => s,
            s => SeedPrices.TryGetValue(s, out var seed) ? seed : DefaultSeedPrice); // C# 7 (2017): out var

        // Working copy of current prices for this subscription (mutates on each tick).
        var prices = new Dictionary<string, double>(basePrices); 

        var random = new Random(); // One per client, to avoid locking on Random.Shared

        // {Symbols} is a structured log property, Aspire dashboard will show it
        _logger.LogInformation("Subscribe: [{Symbols}]", string.Join(", ", symbols)); 

        try
        {
            while (!context.CancellationToken.IsCancellationRequested) // As long as client is not disconnected
            {
                // NOTE: In real world, here we would get the latest prices from a market data source
                foreach (var symbol in symbols)
                {
                    prices[symbol] = NextRandomWalkPrice(prices[symbol], basePrices[symbol], random);

                    await responseStream.WriteAsync(new PriceTick
                    {
                        Symbol = symbol,
                        Price = Math.Round(prices[symbol], 2),
                        TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    });
                }

                await Task.Delay(TickInterval, context.CancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Subscribe cancelled by client.");
        }
    }

    private static double NextRandomWalkPrice(double current, double basePrice, Random random)
    {
        // Simplified on purpose to remember it easier

        const double maxDeviation = 0.50; // Never more or less than ±N% of the base price
        const double maxStep = 0.35;     

        var lowerBound = basePrice * (1.0 - maxDeviation);
        var upperBound = basePrice * (1.0 + maxDeviation);

        var roll = random.NextDouble();         // [0, 1)
        var direction = roll * 2.0 - 1.0;       // [-1, 1)
        var stepPercent = direction * maxStep;  // [-N%, +N%]

        var next = current * (1.0 + stepPercent);

        return Math.Clamp(next, lowerBound, upperBound);
    }
}
