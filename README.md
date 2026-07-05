# gRPC all-in-one on .NET: meme trading!

A meme "stock exchange": one .NET service **streams** live prices over gRPC,
another consumes it, revalues a portfolio, and pushes to UI — all orchestrated with **.NET Aspire**.

<img src="images/for-github.png" alt="Test screenshot" width="838" />

## Grpc_Contracts

- The `.proto` contract for a single **server-streaming** RPC: the client sends a
  list of symbols and receives a continuous stream of price ticks for them.
- Compiled once (`GrpcServices="Both"`) and shared by server and client, so they can never drift apart.

## Grpc_Server_Trading — runs the gRPC server

- HTTP/2 is enabled for Kestrel in `appsettings.json` (h2c, no dev-cert hassle).
- `T_Service` overrides `Subscribe` from the contract:
  - seeds default prices and streams them as a bounded **±N% random walk**;
  - logs through an injected `ILogger<T_Service>` (visible in the Aspire dashboard);
  - loops until the `CancellationToken` fires, writing to the response stream on an interval;
  - honoring that token is what makes client disconnects clean.
- Wired in `Program.cs` via `.MapGrpcService<T_Service>()`.

## Grpc_Client_Portfolio — polls the server, stores state, broadcasts

- **`P_Store`** — the portfolio's current state:
  - a single instance per app, holding the user's positions;
  - a thread-safe `ConcurrentDictionary` of prices, shared between the gRPC side and the SignalR side;
  - essentially a container/adapter: user data on one side, gRPC on the other, a snapshot for the UI.
- **`P_Hub`** — a SignalR hub that keeps a reference to `P_Store` to send each client a snapshot at connect.
- **`P_Access`** — a `BackgroundService` that starts immediately and:
  - resolves the gRPC server address;
  - opens a disposable gRPC channel to it;
  - instantiates the proto-generated client;
  - builds a `Subscribe` request and calls it, passing the `CancellationToken`;
  - reads the stream with `await foreach`, updating the store and broadcasting over SignalR.

## Aspire_AppHost — orchestrates

- `.WithReference(trading)` lets `portfolio` discover how to reach `trading` (via the Aspire DCP):
  it injects the environment variable `services__trading__http__0` with the server's address.

## Run

```bash
dotnet run --project AspNetGrpcTrading/Aspire_AppHost
```

> How this repo was scaffolded from empty templates → [SCAFFOLDING.md](SCAFFOLDING.md)

