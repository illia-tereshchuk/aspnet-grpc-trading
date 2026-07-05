using Portfolio;

var builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults(); // Aspire

builder.Services.AddSignalR();

// Cross-service source of truth for all
builder.Services.AddSingleton<P_Store>();

// Runs in background immediately
// Reads gRPC stream
// Updates PortfolioState
// Pushes to SignalR
builder.Services.AddHostedService<P_Access>();

var app = builder.Build();

app.MapDefaultEndpoints();  // /health and /alive

// NOTE: .Use* is for middleware
// Order of middleware matters
app.UseDefaultFiles();  // Middleware to satisfy "/" with "wwwroot/index.html"
app.UseStaticFiles();   // Middleware to give "index.html" to browser

app.MapHub<P_Hub>("/hub/portfolio");

app.Run();
