using ModelContextProtocol.Client;
using OpenAI.Responses;

namespace ChatBot.Traditional;

/// <summary>
/// Extension methods that bridge <see cref="McpClientTool"/> and <see cref="McpClient"/> to the
/// OpenAI Responses API surface (<see cref="FunctionTool"/>).
/// </summary>
public static class McpClientToolExtensions
{
    extension(McpClientTool t)
    {
        /// <summary>
        /// Converts an <see cref="McpClientTool"/> to an OpenAI <see cref="FunctionTool"/> by
        /// mapping the MCP tool's name, description, and JSON schema. Strict mode is disabled
        /// because MCP schemas may contain constructs not supported by OpenAI strict mode.
        /// </summary>
        public FunctionTool ToFunctionTool() => ResponseTool.CreateFunctionTool(
            functionName: t.Name,
            functionDescription: t.Description,
            functionParameters: BinaryData.FromString(
                t.JsonSchema.GetRawText()),
            strictModeEnabled: false);
    }

    extension(McpClient client)
    {
        /// <summary>
        /// Fetches all tools offered by the MCP server and returns them as an array of
        /// <see cref="FunctionTool"/> instances suitable for the OpenAI Responses API.
        /// </summary>
        public async Task<FunctionTool[]> ListFunctionTools() =>
            [.. (await client.ListToolsAsync()).Select(t => t.ToFunctionTool())];
    }
}
