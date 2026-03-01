using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace ChatBot.AgentFramework;

/// <summary>
/// Extension methods that bridge <see cref="McpToolsProvider"/> to the Microsoft.Extensions.AI
/// surface (<see cref="AITool"/>).
/// </summary>
public static class McpToolsExtensions
{
    extension(McpToolsProvider provider)
    {
        /// <summary>
        /// Returns MCP tools as <see cref="AITool"/> instances for the Microsoft.Extensions.AI
        /// agent-framework implementation. Because <see cref="McpClientTool"/> already implements
        /// <see cref="AITool"/>, the conversion is a simple cast with no additional wrapping.
        /// A fresh <c>ListToolsAsync</c> call is made on every invocation.
        /// </summary>
        /// <returns>An array of <see cref="AITool"/> objects ready to be passed to an AI pipeline.</returns>
        public async Task<AITool[]> GetMcpToolsAsAIToolsAsync()
        {
            var client = await provider.GetClientAsync();
            var tools = await client.ListToolsAsync();
            return [.. tools.Cast<AITool>()];
        }
    }
}
