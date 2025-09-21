using System.Diagnostics;
using ChatBotDb;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OpenAI.Responses;

namespace ChatBot;

public static class ConversationEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapConversationsEndpoints()
        {
            var api = app.MapGroup("/conversations");
            api.MapPost("/", AddConversation);
            api.MapPost("/{conversationId}/chat", Chat);

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
        IConversationRepository context,
        ActivitySource source,
        OpenAIManager openAIManager,
        int conversationId,
        NewMessageRequest request,
        CancellationToken cancellationToken)
    {
        using (var span = source.StartActivity("Add message to conversation"))
        {
            try
            {
                var userMessage = ResponseItem.CreateUserMessageItem(request.Message);
                await context.AddResponseToConversation(conversationId, userMessage);
            }
            catch (ConversionNotFoundException)
            {
                span?.SetStatus(ActivityStatusCode.Error, "Conversation not found");
                return Results.NotFound();
            }
        }


        return TypedResults.ServerSentEvents(openAIManager.GetAssistantStreaming(conversationId, cancellationToken), eventType: "textDelta");
    }

    public record NewConversationResponse(int ConversationId);

    public record NewMessageRequest(string Message);
    public record NewMessageResponse(int MessageId);

    record AssistantResponseMessage(string DeltaText);
}
