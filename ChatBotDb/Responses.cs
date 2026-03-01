using Microsoft.EntityFrameworkCore;

namespace ChatBotDb;

public interface ISessionRepository
{
    Task<string?> GetSession(int conversationId);
    Task SaveSession(int conversationId, string sessionData);
}

public class SessionRepository(ApplicationDataContext context) : ISessionRepository
{
    public async Task<string?> GetSession(int conversationId)
    {
        var conversation = await context.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new ConversationNotFoundException();
        return conversation.SessionData;
    }

    public async Task SaveSession(int conversationId, string sessionData)
    {
        var conversation = await context.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new ConversationNotFoundException();
        conversation.SessionData = sessionData;
        await context.SaveChangesAsync();
    }
}

public class ConversationNotFoundException : Exception
{
    public ConversationNotFoundException() { }
    public ConversationNotFoundException(string message) : base(message) { }
    public ConversationNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
