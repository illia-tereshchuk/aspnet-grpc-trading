// A very first service which starts in solution, gRPC server
using Trading;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(); // Aspire: telemetry, health checks, service discovery.

builder.Services.AddGrpc();

var app = builder.Build(); // BEFORE .Build() services are always added (Joda)

app.MapDefaultEndpoints(); // Adds "/health" and "/alive", check it

app.MapGrpcService<T_Service>(); // Implemented here, derived from .proto "TradingFeed.TradingFeedBase"

app.MapGet("/", () => "TradingFeed gRPC server. Use a gRPC client to call Subscribe."); // Minimal API since .NET 6

app.Run();
