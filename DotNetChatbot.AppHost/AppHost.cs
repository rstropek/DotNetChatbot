using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var sqlite = builder.AddSqlite(
    "chatbot-db",
    builder.Configuration["Database:path"],
    builder.Configuration["Database:fileName"])
    .WithSqliteWeb(); // optionally add web admin UI (requires Docker/podman)

var apiKey = builder.AddParameter("openai-api-key", secret: true);
var model = builder.AddParameter("openai-model", "gpt-5");

var cartMcp = builder.AddProject<Projects.CartMcp>("cart-mcp")
    .WithReference(sqlite);

var chatbot = builder.AddProject<Projects.ChatBot>("chatbot")
    .WithReference(sqlite)
    .WithReference(cartMcp)
    .WithEnvironment("OPENAI_API_KEY", apiKey)
    .WithEnvironment("OPENAI_MODEL", model);

builder.AddNpmApp("chat-ui", "../ChatUI")
    .WithReference(chatbot)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
