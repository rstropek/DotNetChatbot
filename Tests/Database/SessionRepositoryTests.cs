using ChatBotDb;

namespace Tests.Database;

public class SessionRepositoryTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    private ApplicationDataContext CreateContext() => new(fixture.Options);

    private async Task<int> CreateConversation(string? sessionData = null)
    {
        using var context = CreateContext();
        var conversation = new Conversation { SessionData = sessionData };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        return conversation.Id;
    }

    [Fact]
    public async Task GetSession_ReturnsNull_WhenConversationHasNoSessionData()
    {
        var id = await CreateConversation();
        using var context = CreateContext();
        var repo = new SessionRepository(context);

        var result = await repo.GetSession(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSession_ReturnsSessionData_WhenConversationHasSessionData()
    {
        var expectedData = """{"messages":["hello"]}""";
        var id = await CreateConversation(expectedData);
        using var context = CreateContext();
        var repo = new SessionRepository(context);

        var result = await repo.GetSession(id);

        Assert.Equal(expectedData, result);
    }

    [Fact]
    public async Task GetSession_ThrowsConversationNotFoundException_WhenConversationDoesNotExist()
    {
        using var context = CreateContext();
        var repo = new SessionRepository(context);

        await Assert.ThrowsAsync<ConversationNotFoundException>(() => repo.GetSession(999));
    }

    [Fact]
    public async Task SaveSession_PersistsSessionData()
    {
        var id = await CreateConversation();
        var sessionData = """{"messages":["hi","there"]}""";

        using (var context = CreateContext())
        {
            var repo = new SessionRepository(context);
            await repo.SaveSession(id, sessionData);
        }

        using (var context = CreateContext())
        {
            var conversation = await context.Conversations.FindAsync(id);
            Assert.Equal(sessionData, conversation!.SessionData);
        }
    }

    [Fact]
    public async Task SaveSession_OverwritesExistingSessionData()
    {
        var id = await CreateConversation("old-data");
        var newData = "new-data";

        using (var context = CreateContext())
        {
            var repo = new SessionRepository(context);
            await repo.SaveSession(id, newData);
        }

        using (var context = CreateContext())
        {
            var conversation = await context.Conversations.FindAsync(id);
            Assert.Equal(newData, conversation!.SessionData);
        }
    }

    [Fact]
    public async Task SaveSession_ThrowsConversationNotFoundException_WhenConversationDoesNotExist()
    {
        using var context = CreateContext();
        var repo = new SessionRepository(context);

        await Assert.ThrowsAsync<ConversationNotFoundException>(
            () => repo.SaveSession(999, "data"));
    }
}
