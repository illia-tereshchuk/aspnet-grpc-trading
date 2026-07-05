// A model of domain. "Trading" produces raw prices, impact (price, value) is calculated here
// Container-adapter, which holds user's data, accepts from gRPC, and mixes it to broadcast to UI

using System.Collections.Concurrent; // For "ConcurrentDictionary"

namespace Portfolio;

public sealed record HoldingItem(   // "What we bought"
    string Symbol,                  // What do we hold
    int Quantity,                   // How much
    double AvgCost);                // For what average price we bought it

public sealed record PositionView(                  // "How it looks now"
    string Symbol, int Quantity, double AvgCost,    // Same to "HoldingItem"
    double Price, double Value, double Pnl);        // Current price, current value, current profit/loss

public sealed record PortfolioSnapshot( // "All together"
    double TotalValue,
    double TotalPnl,
    IReadOnlyList<PositionView> Positions);

public sealed class P_Store // Singleton in "Program.cs", shared between "P_Access" and "P_Hub"
{
    private static readonly HoldingItem[] Holdings = // Collection expression (C# 12, 2023)
    [
        new("DOGE", 10, 142.0), // Target-typed new (C# 9). AvgCost == сідова ціна → P&L стартує з ~0.
        new("HRLD", 5, 175.0),
        new("SDFG", 8, 133.0),
        new("FNDG", 12, 190.0),
        new("MNKY", 7, 120.0),
        new("KRMT", 50, 115.0),
        new("WMNY", 6, 145.0),
    ];

    private readonly ConcurrentDictionary<string, double> _currentPrices = // Start with AvgCost, until first tick arrives
        new(Holdings.ToDictionary(h => h.Symbol, h => h.AvgCost));

    public IReadOnlyList<string> Symbols { get; } = // Get-only auto-property (C# 6)
        Holdings.Select(h => h.Symbol).ToArray();   // Symbols which we listen to

    public void UpdateCurrentPrice(string symbol, double price) => // Thread safe with "ConcurrentDictionary"
        _currentPrices[symbol] = price;

    public PortfolioSnapshot Snapshot() // Used for SignalR broadcast
    {
        var positions = new List<PositionView>(Holdings.Length);
        double totalValue = 0, totalPnl = 0;

        foreach (var holding in Holdings)
        {
            var priceOfSymbol = _currentPrices.GetValueOrDefault(holding.Symbol, holding.AvgCost);
            var valueOfPosition = holding.Quantity * priceOfSymbol;
            var pnl = holding.Quantity * (priceOfSymbol - holding.AvgCost);

            totalValue += valueOfPosition;
            totalPnl += pnl;

            positions.Add(new PositionView(
                holding.Symbol, holding.Quantity, holding.AvgCost,
                Math.Round(priceOfSymbol, 2), Math.Round(valueOfPosition, 2), Math.Round(pnl, 2)));
        }

        return new PortfolioSnapshot(Math.Round(totalValue, 2), Math.Round(totalPnl, 2), positions);
    }
}
