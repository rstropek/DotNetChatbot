using System.Diagnostics;
using ChatBot;
using ChatBot.Traditional;
using ChatBot.AgentFramework;
using ChatBotDb;
using OpenAI.Responses;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqliteDbContext<ApplicationDataContext>("chatbot-db");
builder.Services.AddScoped<ISessionRepository, SessionRepository>();

// Shared: MCP tools provider used by both implementations
builder.Services.AddSingleton<McpToolsProvider>();
builder.Services.AddSingleton<DeveloperMessageProvider>();

// Traditional implementation services
builder.Services.AddScoped<OpenAIManager>();
builder.Services.AddSingleton(_ => new ResponsesClient(
    builder.Configuration["OPENAI_MODEL"] ?? throw new InvalidOperationException("OPENAI_MODEL not set"),
    new System.ClientModel.ApiKeyCredential(builder.Configuration["OPENAI_API_KEY"]!)));
builder.Services.AddSingleton(_ => new ActivitySource(
    builder.Configuration["OTEL_SERVICE_NAME"] ?? throw new InvalidOperationException("OTEL_SERVICE_NAME not set")));

// Agent Framework implementation services
builder.Services.AddSingleton<AgentManager>();

builder.Services.AddCors();

var app = builder.Build();

await app.Services.ApplyMigrations();

app.UseCors(options => options.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.MapGet("/ping", () => "pong");

// Map both endpoint groups — same SSE format, different URL prefixes
app.MapTraditionalConversationsEndpoints();       // /conversations/...
app.MapAgentFrameworkConversationsEndpoints();    // /af/conversations/...

app.Run();
