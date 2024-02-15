using Microsoft.AspNetCore.SignalR.Client;
using Spectre.Console;
using static Spectre.Console.AnsiConsole;

System.Console.OutputEncoding = System.Text.Encoding.UTF8;

var cts = new CancellationTokenSource();
System.Console.CancelKeyPress += (sender, args) =>
{
    cts.Cancel();
    cts = new CancellationTokenSource();
    args.Cancel = true;
};

MarkupLine("Press [green]Ctrl+C[/] to stop answering.");

while (true)
{
    var message = Ask<string>("[yellow]Enter a question: [/]");
    try
    {
        var connection = new HubConnectionBuilder()
                        .WithUrl("https://localhost:7061/ai")
                        .WithAutomaticReconnect()
                        .Build();

        await connection.StartAsync();
        Write(Emoji.Known.CheckMarkButton);
        using var _ = connection.On<string>("Receive", response => MarkupLine($"[grey]{response}[/]"));

        await foreach (var line in connection.StreamAsync<string>("Ask", message, cts.Token))
        {
            Markup($"[lime]{line}[/]");
        }
    }
    catch (OperationCanceledException) { }
    finally
    {
        WriteLine();
    }
}
    