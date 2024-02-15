using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace StreamAI;

public class AgentHub(IAgent agent) : Hub
{
    public async IAsyncEnumerable<string> Ask(string message, [EnumeratorCancellation] CancellationToken cancellation)
    {
        await foreach (var response in agent.Ask(Clients.Caller, message, cancellation))
            yield return response;
    }

    public async override Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Receive", "Connected to server");
        await base.OnConnectedAsync();
    }
}
