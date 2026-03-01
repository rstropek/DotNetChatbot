using ModelContextProtocol.Client;

namespace ChatBot;

/// <summary>
/// Provides lazy-initialized access to MCP (Model Context Protocol) tools exposed by the
/// shopping cart MCP server. A single <see cref="McpClient"/> is created on first use and
/// reused for the lifetime of the provider. Thread safety during initialization is guaranteed
/// via a <see cref="SemaphoreSlim"/>.
/// </summary>
public class McpToolsProvider(IConfiguration config, ILoggerFactory loggerFactory) : IAsyncDisposable
{
    private McpClient? mcpClient;

    // Guards McpClient initialization against concurrent callers.
    // Initialized with a count of 1 so it acts as a mutex: only one thread
    // may execute the creation logic at a time. Once the client is created,
    // the outer null-check short-circuits all subsequent calls before they
    // ever try to acquire the semaphore.
    private readonly SemaphoreSlim semaphore = new(1, 1);

    /// <summary>
    /// Returns the shared <see cref="McpClient"/>, creating and connecting it on the first call.
    /// Subsequent calls return the cached instance without acquiring the semaphore.
    /// </summary>
    public async Task<McpClient> GetClientAsync()
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
