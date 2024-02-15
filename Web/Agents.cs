using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using CheBot;

namespace StreamAI;

public interface IAgent
{
    IAsyncEnumerable<string> Ask(IClientProxy client, string message, CancellationToken cancellation);
}

public class TokenizingAgent(IAgent agent) : IAgent
{
    public async IAsyncEnumerable<string> Ask(IClientProxy client, string message, [EnumeratorCancellation] CancellationToken cancellation)
    {
        var tokens = 0;
        cancellation.Register(() => client.SendAsync("Receive", $"\r\nTokens used: {tokens}"));

        await foreach (var response in agent.Ask(client, message, cancellation))
        {
            tokens += await Tokenizer.Default.MeasureAsync("gpt-4-1106-preview", response);
            yield return response;
        }

        await client.SendAsync("Receive", $"\r\nTokens used: {tokens}");
    }
}

public class HttpAgent(IHttpClientFactory httpFactory, IConfiguration configuration) : IAgent
{
    const string Url = "https://api.openai.com/v1/chat/completions";

    public async IAsyncEnumerable<string> Ask(IClientProxy client, string message, [EnumeratorCancellation] CancellationToken cancellation)
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
                Authorization = new AuthenticationHeaderValue("Bearer", configuration["OpenAI:Key"] ??
                    throw new InvalidOperationException("Please provide the OpenAI API key. See readme for more information.")),
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