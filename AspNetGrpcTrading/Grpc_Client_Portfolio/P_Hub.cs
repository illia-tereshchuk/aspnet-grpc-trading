using Microsoft.AspNetCore.SignalR;

namespace Portfolio;

// "Hub" works per one connection
// "IHubContext<P_Hub>" works for all connections
public sealed class P_Hub : Hub
{
    private readonly P_Store _store;

    public P_Hub(P_Store store) => _store = store;

    public override async Task OnConnectedAsync() // To get first snapshot immediately
    {
        await Clients.Caller.SendAsync("snapshot", _store.Snapshot()); // Sent only once to particular caller
        await base.OnConnectedAsync();
    }
}
