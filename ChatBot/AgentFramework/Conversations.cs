using ChatBotDb;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ChatBot.AgentFramework;

public static class AgentFrameworkConversationEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapAgentFrameworkConversationsEndpoints()
        {
            var api = app.MapGroup("/af/conversations");
            api.MapPost("/", AddConversation);
            api.MapPost("/{conversationId}/chat", Chat);
            api.MapGet("/{conversationId}", GetHistory);

            return app;
        }
    }

    public async static Task<Created<NewConversationResponse>> AddConversation(ApplicationDataContext context)
    {
        // Same as traditional — just creates a DB row to get a conversation ID
        var conversation = new Conversation();
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        return TypedResults.Created(null as string, new NewConversationResponse(conversation.Id));
    }

    public static async Task<IResult> Chat(
        AgentManager agentManager,
        ISessionRepository sessionRepository,
        int conversationId,
        NewMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.BadRequest("Message must not be empty.");
        }

        // Restore session from DB or create a new one
        string? serializedSession;
        try
        {
            serializedSession = await sessionRepository.GetSession(conversationId);
        }
        catch (ConversationNotFoundException)
        {
            return Results.NotFound();
        }

        var session = await agentManager.RestoreOrCreateSessionAsync(
            serializedSession, cancellationToken);

        // Stream the response, then persist session state to DB
        return TypedResults.ServerSentEvents(
            StreamAndPersist(agentManager, sessionRepository, conversationId, session, request.Message, cancellationToken),
            eventType: "textDelta");
    }

    private static async IAsyncEnumerable<AssistantResponseMessage> StreamAndPersist(
        AgentManager agentManager,
        ISessionRepository sessionRepository,
        int conversationId,
        Microsoft.Agents.AI.AgentSession session,
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var update in agentManager.GetAssistantStreaming(
            userMessage, session, cancellationToken))
        {
            yield return update;
        }

        // Persist the full session (including tool calls) to DB after streaming completes
        var serialized = await agentManager.SerializeSessionAsync(session);
        await sessionRepository.SaveSession(conversationId, serialized);
    }

    public static async Task<IResult> GetHistory(
        AgentManager agentManager,
        ISessionRepository sessionRepository,
        int conversationId,
        CancellationToken cancellationToken)
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

        var session = await agentManager.RestoreOrCreateSessionAsync(
            serializedSession, cancellationToken);
        var messages = await agentManager.GetHistoryMessages(session);
        return Results.Ok(messages.Select(m => new { role = m.Role, content = m.Content }));
    }

    public record NewConversationResponse(int ConversationId);

    public record NewMessageRequest(string Message);
}
