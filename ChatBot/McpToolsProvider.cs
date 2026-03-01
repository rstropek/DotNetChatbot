using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Responses;

namespace ChatBot;

public class McpToolsProvider(IConfiguration config, ILoggerFactory loggerFactory) : IAsyncDisposable
{
    private McpClient? mcpClient;
    private FunctionTool[]? mcpTools;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    private async Task<McpClient> GetClientAsync()
    {
        if (mcpClient != null)
        {
            return mcpClient;
        }

        await semaphore.WaitAsync();
        try
        {
#pragma warning disable CA1508 // Double-check lock pattern — inner null check is intentional for thread safety
            if (mcpClient != null)
            {
                return mcpClient;
            }
#pragma warning restore CA1508

#pragma warning disable CA2000 // Transport ownership is transferred to McpClient, disposed via DisposeAsync
            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(config["Services:cart-mcp:http:0"]!),
                Name = "Shopping Cart MCP"
            });
#pragma warning restore CA2000
            mcpClient = await McpClient.CreateAsync(transport, loggerFactory: loggerFactory);
            return mcpClient;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Returns MCP tools as OpenAI FunctionTool[] for the traditional implementation.
    /// </summary>
    public async Task<(McpClient Client, FunctionTool[] Tools)> GetToolsAsync()
    {
        var client = await GetClientAsync();

        if (mcpTools != null)
        {
            return (client, mcpTools);
        }

        mcpTools = await client.ListFunctionTools();
        if (mcpTools.Length == 0)
        {
            throw new InvalidOperationException("MCP server offers no tools, this should never happen");
        }

        return (client, mcpTools);
    }

    /// <summary>
    /// Returns MCP tools as AITool[] for the Agent Framework implementation.
    /// McpClientTool implements AITool, so the conversion is trivial.
    /// </summary>
    public async Task<AITool[]> GetMcpToolsAsAIToolsAsync()
    {
        var client = await GetClientAsync();
        var tools = await client.ListToolsAsync();
        return [.. tools.Cast<AITool>()];
    }

    public async ValueTask DisposeAsync()
    {
        if (mcpClient != null)
        {
            await mcpClient.DisposeAsync();
        }

        semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}

public static class McpClientToolExtensions
{
    extension(McpClientTool t)
    {
        public FunctionTool ToFunctionTool() => ResponseTool.CreateFunctionTool(
            functionName: t.Name,
            functionDescription: t.Description,
            functionParameters: BinaryData.FromString(
                t.JsonSchema.GetRawText()),
            strictModeEnabled: false);
    }

    extension(McpClient client)
    {
        public async Task<FunctionTool[]> ListFunctionTools() =>
            [.. (await client.ListToolsAsync()).Select(t => t.ToFunctionTool())];
    }
}
