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
    private const string GetConversationHistoryRouteName = "GetConversationHistory";

    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapTraditionalConversationsEndpoints()
        {
            var api = app.MapGroup("/conversations");
            api.MapPost("/", AddConversation);
            api.MapPost("/{conversationId}/chat", Chat);
            api.MapGet("/{conversationId}", GetHistory).WithName(GetConversationHistoryRouteName);

            return app;
        }
    }

    public async static Task<Created<NewConversationResponse>> AddConversation(
        ApplicationDataContext context,
        LinkGenerator linkGenerator)
    {
        var conversation = new Conversation();
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        var url = linkGenerator.GetPathByName(GetConversationHistoryRouteName, new { conversationId = conversation.Id });
        return TypedResults.Created(url, new NewConversationResponse(conversation.Id));
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

        var messages = conversation
            .OfType<MessageResponseItem>()
            .Where(m => m.Role is MessageRole.User or MessageRole.Assistant)
            .Select(m => new
            {
                role = m.Role == MessageRole.User ? "user" : "assistant",
                content = m.Content
                    .Select(c => c.Text)
                    .FirstOrDefault(t => t is not null)
            })
            .Where(m => m.content is not null)
            .ToList();

        return Results.Ok(messages);
    }


    private static List<ResponseItem> DeserializeSession(string? serializedSession)
    {
        if (serializedSession is null) { return []; }

        using var doc = JsonDocument.Parse(serializedSession);
        return [.. doc.RootElement.EnumerateArray()
            .Select(e => ModelReaderWriter.Read<ResponseItem>(
                BinaryData.FromString(e.GetRawText()), ModelReaderWriterOptions.Json)!)];
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


    public record NewConversationResponse(int ConversationId);

    public record NewMessageRequest(string Message);
}
