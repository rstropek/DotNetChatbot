using System.Buffers;
using System.ClientModel.Primitives;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ChatBotDb;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenAI.Responses;

namespace ChatBot.Traditional;

public static class ConversationEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapTraditionalConversationsEndpoints()
        {
            var api = app.MapGroup("/conversations");
            api.MapPost("/", AddConversation);
            api.MapPost("/{conversationId}/chat", Chat);
            api.MapGet("/{conversationId}", GetHistory);

            return app;
        }
    }

    public async static Task<Created<NewConversationResponse>> AddConversation(ApplicationDataContext context)
    {
        var conversation = new Conversation();
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        return TypedResults.Created(null as string, new NewConversationResponse(conversation.Id));
    }

    public async static Task<IResult> Chat(
        ISessionRepository sessionRepository,
        OpenAIManager openAIManager,
        int conversationId,
        NewMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.BadRequest("Message must not be empty.");
        }

        // Load session blob from DB
        string? serializedSession;
        try
        {
            serializedSession = await sessionRepository.GetSession(conversationId);
        }
        catch (ConversationNotFoundException)
        {
            return Results.NotFound();
        }

        // Deserialize to List<ResponseItem>
        var conversation = DeserializeSession(serializedSession);

        // Add user message
        conversation.Add(ResponseItem.CreateUserMessageItem(request.Message));

        return TypedResults.ServerSentEvents(
            StreamAndPersist(openAIManager, sessionRepository, conversationId, conversation, cancellationToken),
            eventType: "textDelta");
    }

    private static async IAsyncEnumerable<OpenAIManager.AssistantResponseMessage> StreamAndPersist(
        OpenAIManager openAIManager,
        ISessionRepository sessionRepository,
        int conversationId,
        List<ResponseItem> conversation,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var update in openAIManager.GetAssistantStreaming(conversation, cancellationToken))
        {
            yield return update;
        }

        // After streaming completes, serialize and persist the full conversation
        var serialized = SerializeSession(conversation);
        await sessionRepository.SaveSession(conversationId, serialized);
    }

    public static async Task<IResult> GetHistory(
        ISessionRepository sessionRepository,
        int conversationId)
    {
        string? serializedSession;
        try
        {
            serializedSession = await sessionRepository.GetSession(conversationId);
        }
        catch (ConversationNotFoundException)
        {
            return Results.NotFound();
        }

        if (serializedSession is null)
        {
            return Results.Ok(Array.Empty<object>());
        }

        var conversation = DeserializeSession(serializedSession);

        var messages = new List<object>();
        foreach (var item in conversation)
        {
            var itemJson = SerializeResponseItem(item);
            using var doc = JsonDocument.Parse(itemJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeProp))
            {
                var type = typeProp.GetString();
                if (type == "message" && root.TryGetProperty("role", out var roleProp))
                {
                    var role = roleProp.GetString();
                    if (role is "user" or "assistant")
                    {
                        var text = ExtractTextContent(root);
                        if (text is not null)
                        {
                            messages.Add(new { role, content = text });
                        }
                    }
                }
            }
        }

        return Results.Ok(messages);
    }

    private static string? ExtractTextContent(JsonElement root)
    {
        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text))
                {
                    return text.GetString();
                }
            }
        }

        return null;
    }

    private static List<ResponseItem> DeserializeSession(string? serializedSession)
    {
        if (serializedSession is null) { return []; }

        using var doc = JsonDocument.Parse(serializedSession);
        var result = new List<ResponseItem>();
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var item = ModelReaderWriter.Read<ResponseItem>(
                BinaryData.FromString(element.GetRawText()),
                ModelReaderWriterOptions.Json)!;
            result.Add(item);
        }
        return result;
    }

    private static string SerializeSession(List<ResponseItem> conversation)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();
            foreach (var item in conversation)
            {
                var itemAsJson = item as IJsonModel<ResponseItem>;
                itemAsJson!.Write(writer, ModelReaderWriterOptions.Json);
            }
            writer.WriteEndArray();
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string SerializeResponseItem(ResponseItem item)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            var itemAsJson = item as IJsonModel<ResponseItem>;
            itemAsJson!.Write(writer, ModelReaderWriterOptions.Json);
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public record NewConversationResponse(int ConversationId);

    public record NewMessageRequest(string Message);
}
