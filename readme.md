# StreamAI

An example using a minimal ASP.NET Core server to stream responses 
from OpenAI to a console app, over SignalR.

Implemented using the suggested approach in the 
[Microsoft Copilot implementation blog post](https://devblogs.microsoft.com/dotnet/building-ai-powered-bing-chat-with-signalr-and-other-open-source-tools/#deep-dive-how-do-we-use-signalr),
using no third-party libraries (even for OpenAI) beyond plain SignalR.