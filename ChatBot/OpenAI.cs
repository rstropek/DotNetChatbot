using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ChatBotDb;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI.Responses;

namespace ChatBot;

public class OpenAIManager(OpenAIResponseClient client, IConversationRepository context,
    IConfiguration config, ILoggerFactory loggerFactory)
{
    private FunctionTool[]? mcpTools = null;
    private string? developerMessage = null;

    public async IAsyncEnumerable<AssistantResponseMessage> GetAssistantStreaming(
        int conversationId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Get conversation history from database
        var conversation = await context.GetConversation(conversationId);

        // Get tools provided by MCP server
        IMcpClient? mcpClient = null;
        if (mcpTools == null)
        {
            mcpClient = await GetMcpClient();
            mcpTools = await mcpClient.ListFunctionTools();
            if (mcpTools.Length == 0)
            {
                throw new InvalidOperationException("MCP server offers no tools, this should never happen");
            }
        }

        // We loop until no more function calls are required
        bool requiresAction;
        do
        {
            requiresAction = false;

            var options = await GetResponseCreationOptions();

            var response = client.CreateResponseStreamingAsync(conversation, options, cancellationToken);
            await foreach (var chunk in response)
            {
                if (cancellationToken.IsCancellationRequested) { yield break; }

                if (chunk is StreamingResponseOutputTextDeltaUpdate textDelta)
                {
                    // We got a chunk of text from the LLM, let's send it to the client
                    yield return new AssistantResponseMessage(textDelta.Delta);
                }

                if (chunk is StreamingResponseOutputItemDoneUpdate doneUpdate
                    && doneUpdate.Item is not ReasoningResponseItem)
                {
                    // We got a final item from the LLM, let's store it in the database
                    await context.AddResponseToConversation(conversationId, doneUpdate.Item);
                    conversation.Add(doneUpdate.Item);

                    // The response might be a function call, in which case we need to execute it
                    if (doneUpdate.Item is FunctionCallResponseItem functionCall)
                    {
                        requiresAction = true;
                        FunctionCallOutputResponseItem functionResult;

                        // For demo purposes, we notify the client of the function call
                        yield return new AssistantResponseMessage($"""
                            
                            ```txt
                            {functionCall.FunctionName}({functionCall.FunctionArguments})
                            ```
                            
                            """);

                        switch (functionCall.FunctionName)
                        {
                            case nameof(ProductsTools.GetAvailableColorsForFlower):
                                // This demonstrates how to call a local function tool
                                var argument = JsonSerializer.Deserialize<ProductsTools.GetAvailableColorsForFlowerRequest>(functionCall.FunctionArguments)!;
                                var availableColors = ProductsTools.GetAvailableColorsForFlower(argument);
                                var availableColorsJson = JsonSerializer.Serialize(availableColors);
                                functionResult = new FunctionCallOutputResponseItem(functionCall.CallId, availableColorsJson);
                                break;

                            // Here we could add additional function tools

                            default:
                                // This demonstrates how to call a tool provided by the MCP server
                                var mcpTool = mcpTools!.FirstOrDefault(t => t.FunctionName == functionCall.FunctionName);
                                if (mcpTool != null)
                                {
                                    mcpClient ??= await GetMcpClient();
                                    var functionArguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(functionCall.FunctionArguments)!;
                                    var callResult = await mcpClient.CallToolAsync(functionCall.FunctionName, functionArguments);
                                    functionResult = new FunctionCallOutputResponseItem(functionCall.CallId, (callResult.Content[0] as TextContentBlock)?.Text ?? "");
                                }
                                else
                                {
                                    functionResult = new FunctionCallOutputResponseItem(functionCall.CallId, "Function not found");
                                }

                                break;
                        }

                        await context.AddResponseToConversation(conversationId, functionResult);
                        conversation.Add(functionResult);
                    }
                }
            }
        }
        while (requiresAction);
    }

    private async Task<ResponseCreationOptions> GetResponseCreationOptions()
    {
        developerMessage ??= await GetDeveloperMessage();
        var options = new ResponseCreationOptions
        {
            Instructions = developerMessage,
            ReasoningOptions = new()
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.Low
            },
            MaxOutputTokenCount = 2500,
            StoredOutputEnabled = false,
            Tools = { ProductsTools.GetAvailableColorsForFlowerTool }
        };

        if (mcpTools != null)
        {
            foreach (var tool in mcpTools) { options.Tools.Add(tool); }
        }

        return options;
    }

    private async static Task<string> GetDeveloperMessage()
    {
        const string resourceName = "ChatBot.developer-message.md";

        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Resource {resourceName} not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private async Task<IMcpClient> GetMcpClient()
    {
        // Note that the names of these classes will change in the near future.
        // They have already been change in the GitHub repo, but not yet in the NuGet package.
        var transport = new SseClientTransport(new()
        {
            Endpoint = new Uri(config["Services:cart-mcp:http:0"]!),
            Name = "Shopping Cart MCP"
        }, loggerFactory);
        return await McpClientFactory.CreateAsync(transport, loggerFactory: loggerFactory);
    }

    public record AssistantResponseMessage(string DeltaText);
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

    extension(IMcpClient mcpClient)
    {
        public async Task<FunctionTool[]> ListFunctionTools() =>
            [.. (await mcpClient.ListToolsAsync()).Select(t => t.ToFunctionTool())];
    }
}