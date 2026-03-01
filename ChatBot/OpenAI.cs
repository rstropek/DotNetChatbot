using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ChatBotDb;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI.Responses;

namespace ChatBot;

public class McpToolsProvider(IConfiguration config, ILoggerFactory loggerFactory) : IAsyncDisposable
{
    private McpClient? mcpClient;
    private FunctionTool[]? mcpTools;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task<(McpClient Client, FunctionTool[] Tools)> GetToolsAsync()
    {
        if (mcpClient != null && mcpTools != null)
        {
            return (mcpClient, mcpTools);
        }

        await semaphore.WaitAsync();
        try
        {
            if (mcpClient != null && mcpTools != null)
            {
                return (mcpClient, mcpTools);
            }

            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri(config["Services:cart-mcp:http:0"]!),
                Name = "Shopping Cart MCP"
            });
            mcpClient = await McpClient.CreateAsync(transport, loggerFactory: loggerFactory);
            mcpTools = await mcpClient.ListFunctionTools();
            if (mcpTools.Length == 0)
            {
                throw new InvalidOperationException("MCP server offers no tools, this should never happen");
            }

            return (mcpClient, mcpTools);
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

public class OpenAIManager(ResponsesClient client, IConversationRepository context,
    IConfiguration config, McpToolsProvider mcpToolsProvider)
{
    private static readonly Lazy<Task<string>> developerMessage = new(LoadDeveloperMessage);
    private string Model => config["OPENAI_MODEL"] ?? throw new InvalidOperationException("OPENAI_MODEL not set");

    public async IAsyncEnumerable<AssistantResponseMessage> GetAssistantStreaming(
        int conversationId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Get conversation history from database
        var conversation = await context.GetConversation(conversationId);

        // Get tools provided by MCP server
        var (mcpClient, mcpTools) = await mcpToolsProvider.GetToolsAsync();

        // We loop until no more function calls are required
        bool requiresAction;
        do
        {
            requiresAction = false;

            var options = await GetResponseCreationOptions(conversation, mcpTools);

            var response = client.CreateResponseStreamingAsync(options, cancellationToken);
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
                                var mcpTool = mcpTools.FirstOrDefault(t => t.FunctionName == functionCall.FunctionName);
                                if (mcpTool != null)
                                {
                                    var functionArguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(functionCall.FunctionArguments)!;
                                    var callResult = await mcpClient.CallToolAsync(functionCall.FunctionName, functionArguments, cancellationToken: cancellationToken);
                                    var resultText = callResult.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "";
                                    functionResult = new FunctionCallOutputResponseItem(functionCall.CallId, resultText);
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

    private async Task<CreateResponseOptions> GetResponseCreationOptions(List<ResponseItem> conversation, FunctionTool[] mcpTools)
    {
        var options = new CreateResponseOptions(Model, conversation)
        {
            Instructions = await developerMessage.Value,
            ReasoningOptions = new()
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.Low
            },
            MaxOutputTokenCount = 2500,
            StoredOutputEnabled = false,
            StreamingEnabled = true,
            Tools = { ProductsTools.GetAvailableColorsForFlowerTool }
        };

        foreach (var tool in mcpTools) { options.Tools.Add(tool); }

        return options;
    }

    private static async Task<string> LoadDeveloperMessage()
    {
        const string resourceName = "ChatBot.developer-message.md";

        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Resource {resourceName} not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
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

    extension(McpClient client)
    {
        public async Task<FunctionTool[]> ListFunctionTools() =>
            [.. (await client.ListToolsAsync()).Select(t => t.ToFunctionTool())];
    }
}
