using System.Buffers;
using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenAI.Responses;

namespace ChatBotDb;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public interface IConversationRepository
{
    Task<List<ResponseItem>> GetConversation(int conversationId);
    Task AddResponseToConversation(int conversationId, ResponseItem responseItem);

    async Task AddResponsesToConversation(int conversationId, IEnumerable<ResponseItem> responseItems)
    {
        foreach (var item in responseItems)
        {
            await AddResponseToConversation(conversationId, item);
        }
    }
}

public class ConversationRepository(ApplicationDataContext context) : IConversationRepository
{
    public async Task<List<ResponseItem>> GetConversation(int conversationId)
    {
        var conversation = await context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new ConversionNotFoundException();

        return [.. conversation.Messages
            .OrderBy(m => m.Id)
            .Select(m => ModelReaderWriter.Read<ResponseItem>(
                BinaryData.FromString(m.Content),
                ModelReaderWriterOptions.Json)!)];
    }

    public async Task AddResponseToConversation(int conversationId, ResponseItem responseItem)
    {
        var conversation = await context.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new ConversionNotFoundException();

        var itemAsJson = responseItem as IJsonModel<ResponseItem>;
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            itemAsJson!.Write(writer, ModelReaderWriterOptions.Json);
        }

        var message = new Message
        {
            ConversationId = conversationId,
            Content = Encoding.UTF8.GetString(buffer.WrittenSpan)
        };
        context.Messages.Add(message);
        await context.SaveChangesAsync();
    }
}

public class ConversionNotFoundException : Exception { }
