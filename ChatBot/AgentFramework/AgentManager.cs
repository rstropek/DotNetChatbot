using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;

namespace ChatBot.AgentFramework;

public sealed class AgentManager(IConfiguration config, McpToolsProvider mcpToolsProvider, DeveloperMessageProvider developerMessageProvider) : IDisposable
{
    private string Model => config["OPENAI_MODEL"] ?? throw new InvalidOperationException("OPENAI_MODEL not set");

    private AIAgent? agent;
    private readonly SemaphoreSlim agentLock = new(1, 1);

    private async Task<AIAgent> GetOrCreateAgentAsync()
    {
        if (agent != null) { return agent; }

        await agentLock.WaitAsync();
        try
        {
#pragma warning disable CA1508 // Double-check lock pattern — inner null check is intentional for thread safety
            if (agent != null) { return agent; }
#pragma warning restore CA1508

            // Get MCP tools as AITool[] — no manual JSON schema conversion needed!
            var mcpTools = await mcpToolsProvider.GetMcpToolsAsAIToolsAsync();

            // Build the agent — notice how much simpler this is compared to the traditional approach!
            // No manual tool dispatch loop, no JSON schema generation, no manual MCP tool conversion.
            var builtAgent = new OpenAIClient(config["OPENAI_API_KEY"]!)
                .GetResponsesClient(Model)
                .AsAIAgent(new ChatClientAgentOptions
                {
                    Name = "FlowerShopAssistant",
                    ChatOptions = new()
                    {
                        Instructions = await developerMessageProvider.GetAsync(),
                        Tools = [
                            // Local function tool — just a decorated method, AIFunctionFactory handles the schema
                            AIFunctionFactory.Create(GetAvailableColorsForFlower),
                            .. mcpTools
                        ]
                    }
                });

            // Enable observability — one-liner vs manual ActivitySource spans
            agent = builtAgent.AsBuilder()
                .UseOpenTelemetry(sourceName: "ChatBot.AgentFramework")
                .Build();

            return agent;
        }
        finally
        {
            agentLock.Release();
        }
    }

    /// <summary>
    /// Restores a session from its serialized form, or creates a new one if no data exists.
    /// </summary>
    public async Task<AgentSession> RestoreOrCreateSessionAsync(
        string? serializedSession, CancellationToken cancellationToken)
    {
        var currentAgent = await GetOrCreateAgentAsync();

        if (serializedSession is not null)
        {
            var jsonElement = JsonDocument.Parse(serializedSession).RootElement;
            return await currentAgent.DeserializeSessionAsync(jsonElement, cancellationToken: cancellationToken);
        }

        return await currentAgent.CreateSessionAsync(cancellationToken);
    }

    /// <summary>Serializes the session for durable storage.</summary>
    public async Task<string> SerializeSessionAsync(AgentSession session)
    {
        var currentAgent = await GetOrCreateAgentAsync();
        var jsonElement = await currentAgent.SerializeSessionAsync(session);
        return jsonElement.GetRawText();
    }

    /// <summary>
    /// Extracts user/assistant text messages from the session for the GET history endpoint.
    /// </summary>
    public async Task<List<(string Role, string Content)>> GetHistoryMessages(AgentSession session)
    {
        var currentAgent = await GetOrCreateAgentAsync();
        var provider = currentAgent.GetService<InMemoryChatHistoryProvider>();
        var messages = provider?.GetMessages(session);

        if (messages is null) { return []; }

        var result = new List<(string, string)>();
        foreach (var msg in messages)
        {
            if (msg.Role == ChatRole.User || msg.Role == ChatRole.Assistant)
            {
                var text = msg.Text;
                if (text is { Length: > 0 })
                {
                    result.Add((msg.Role == ChatRole.User ? "user" : "assistant", text));
                }
            }
        }

        return result;
    }

    public async IAsyncEnumerable<AssistantResponseMessage> GetAssistantStreaming(
        string userMessage,
        AgentSession session,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var currentAgent = await GetOrCreateAgentAsync();

        // Stream the response — Agent Framework handles the tool calling loop automatically!
        // The session accumulates all messages (including tool calls) automatically.
        await foreach (var update in currentAgent.RunStreamingAsync(
            userMessage, session, cancellationToken: cancellationToken))
        {
            if (update.Text is { Length: > 0 } text)
            {
                yield return new AssistantResponseMessage(text);
            }
        }
    }

    // Function tool — just a decorated static method, no manual JSON schema needed!
    private static readonly Dictionary<string, List<string>> FlowerColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rose"] = ["red", "yellow", "purple"],
        ["Lily"] = ["yellow", "pink", "white"],
        ["Gerbera"] = ["pink", "red", "yellow"],
        ["Freesia"] = ["white", "pink", "red", "yellow"],
        ["Tulip"] = ["red", "yellow", "purple"],
        ["Sunflower"] = ["yellow"]
    };

    [Description("Gets a list of available colors for a specific flower")]
    static string GetAvailableColorsForFlower(
        [Description("The name of the flower")] string flowerName)
        => FlowerColors.TryGetValue(flowerName, out var colors)
            ? string.Join(", ", colors)
            : "No colors found for this flower.";

    public void Dispose()
    {
        agentLock.Dispose();
        GC.SuppressFinalize(this);
    }

}

public record AssistantResponseMessage(string DeltaText);
