# Scaffolding — how this repo was built from empty templates

This page shows the exact terminal commands you'd run by hand to reproduce the skeleton from scratch.
.NET 10 SDK is needed.

## Commands

```bash
# 0) ONE-TIME: Aspire templates.
#    Since .NET 9+, Aspire ships as a NuGet template package, not a workload.
dotnet new install Aspire.ProjectTemplates

# 1) Solution. On .NET 10 `dotnet new sln` produces a .slnx by default.
dotnet new sln -n AspNetGrpcTrading

# 2) Five projects, each from a different template:
dotnet new aspire-apphost         -o AspNetGrpcTrading/Aspire_AppHost         -n Aspire_AppHost
dotnet new aspire-servicedefaults -o AspNetGrpcTrading/Aspire_ServiceDefaults -n Aspire_ServiceDefaults
dotnet new classlib               -o AspNetGrpcTrading/Grpc_Contracts         -n Grpc_Contracts
dotnet new grpc                   -o AspNetGrpcTrading/Grpc_Server_Trading    -n Grpc_Server_Trading
dotnet new web                    -o AspNetGrpcTrading/Grpc_Client_Portfolio  -n Grpc_Client_Portfolio

# 3) Add every project to the solution:
dotnet sln add AspNetGrpcTrading/Aspire_AppHost AspNetGrpcTrading/Aspire_ServiceDefaults \
               AspNetGrpcTrading/Grpc_Contracts AspNetGrpcTrading/Grpc_Server_Trading \
               AspNetGrpcTrading/Grpc_Client_Portfolio

# 4) Project references:
#    server & client → shared contract + service defaults
dotnet add AspNetGrpcTrading/Grpc_Server_Trading   reference AspNetGrpcTrading/Grpc_Contracts AspNetGrpcTrading/Aspire_ServiceDefaults
dotnet add AspNetGrpcTrading/Grpc_Client_Portfolio reference AspNetGrpcTrading/Grpc_Contracts AspNetGrpcTrading/Aspire_ServiceDefaults
#    apphost → both services (so Aspire can orchestrate them)
dotnet add AspNetGrpcTrading/Aspire_AppHost reference AspNetGrpcTrading/Grpc_Server_Trading AspNetGrpcTrading/Grpc_Client_Portfolio

# 5) NuGet packages:
#    contracts compile the shared .proto (both server base + client stub)
dotnet add AspNetGrpcTrading/Grpc_Contracts package Google.Protobuf
dotnet add AspNetGrpcTrading/Grpc_Contracts package Grpc.Net.Client
dotnet add AspNetGrpcTrading/Grpc_Contracts package Grpc.Tools
#    client creates a GrpcChannel directly
dotnet add AspNetGrpcTrading/Grpc_Client_Portfolio package Grpc.Net.Client
#    (server already has Grpc.AspNetCore — provided by the `grpc` template)
```

## What each template gives you

| Command | What you get out of the box |
|---|---|
| `aspire-apphost` | Orchestrator project with `Aspire.AppHost.Sdk`, empty `AppHost.cs` |
| `aspire-servicedefaults` | `Extensions.cs` with `AddServiceDefaults` / `MapDefaultEndpoints` (telemetry, health checks, service discovery) |
| `grpc` | ASP.NET Core server + `Grpc.AspNetCore` + a sample `Protos/greet.proto` and `GreeterService` |
| `web` | Minimal ASP.NET Core app (`Program.cs` with `Hello World`) |
| `classlib` | Empty class library (used to host the shared `.proto`) |

## What stays manual

- **Delete the `grpc` template sample** (`greet.proto`, `GreeterService.cs`) — replaced by the shared contract in `Grpc_Contracts`.
- **Add the `<Protobuf>` item** to `Grpc_Contracts.csproj` — this is what tells `Grpc.Tools` to compile the proto. 
   ```xml
   <ItemGroup>
     <Protobuf Include="trading.proto" GrpcServices="Both" />
   </ItemGroup>
   ```
   (Also set `<PrivateAssets>all</PrivateAssets>` on the `Grpc.Tools` reference.)
