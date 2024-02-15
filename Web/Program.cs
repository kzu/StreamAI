using StreamAI;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets("df5c8f9e-6b16-43c0-9887-90f27b794d81");
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<HttpAgent>();
builder.Services.AddSingleton<OpenAIAgent>();
builder.Services.AddSingleton<IAgent>(sp => new TokenizingAgent(sp.GetRequiredService<OpenAIAgent>()));

var app = builder.Build();
app.UseHttpsRedirection();

app.MapHub<AgentHub>("ai");

app.Run();