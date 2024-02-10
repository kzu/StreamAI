using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace StreamAI;

public class AgentHub(IHttpClientFactory httpFactory, IConfiguration configuration) : Hub
{
    const string Url = "https://api.openai.com/v1/chat/completions";

    public async override Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Receive", "Connected to server");
        await base.OnConnectedAsync();
    }

    public async IAsyncEnumerable<string> Ask(string message, [EnumeratorCancellation] CancellationToken cancellation)
    {
        var jsonRequest = JsonSerializer.Serialize(new
        {
            model = "gpt-4-1106-preview",
            temperature = 0.7d,
            stream = true,
            messages = new[]
            {
                new { role = "user", content = message }
            },
        });

        using var http = httpFactory.CreateClient();
        var response = await http.SendAsync(new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json"),
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", configuration["OpenAI:Key"]),
            }
        }, HttpCompletionOption.ResponseHeadersRead, cancellation);

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellation));
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellation);
            if (!string.IsNullOrEmpty(line))
            {
                if (line.Contains("data: [DONE]"))
                    break;

                var data = JsonSerializer.Deserialize<Data>(line["data: ".Length..]);
                var text = data?.choices.FirstOrDefault()?.delta.content;
                if (!string.IsNullOrEmpty(text))
                    yield return text;
            }
        }
    }

    record Choice(Delta delta, string? finish_reason);
    record Delta(string? role, string? content);
    record Data(string id, string model, Choice[] choices);
}
