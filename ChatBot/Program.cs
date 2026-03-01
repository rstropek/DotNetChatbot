using System.Diagnostics;
using ChatBot;
using ChatBotDb;
using OpenAI.Responses;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqliteDbContext<ApplicationDataContext>("chatbot-db");
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddSingleton<McpToolsProvider>();
builder.Services.AddScoped<OpenAIManager>();

builder.Services.AddSingleton(_ => new ResponsesClient(
    new System.ClientModel.ApiKeyCredential(builder.Configuration["OPENAI_API_KEY"]!)));

builder.Services.AddSingleton(new ActivitySource(
    builder.Configuration["OTEL_SERVICE_NAME"] ?? throw new InvalidOperationException("OTEL_SERVICE_NAME not set")));

builder.Services.AddCors();

var app = builder.Build();

await app.Services.ApplyMigrations();

app.UseCors(options => options.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.MapConversationsEndpoints();

app.Run();
