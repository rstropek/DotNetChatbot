using System.Buffers;
using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;
using ChatBotDb;
using OpenAI.Responses;

namespace ChatBot;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public static class ChatBotEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapChatEndpoints()
        {
            var api = app.MapGroup("/chat");

            api.MapPost("/", DoChat);

            return app;
        }
    }

    public static IResult DoChat(ApplicationDataContext context)
    {
        ResponseItem item = ResponseItem.CreateUserMessageItem("asdf");
        var item2 = item as IJsonModel<ResponseItem>;
        
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            item2.Write(writer, ModelReaderWriterOptions.Json);
        }

        var json = Encoding.UTF8.GetString(buffer.WrittenSpan);
        item = ModelReaderWriter.Read<ResponseItem>(BinaryData.FromString(json), ModelReaderWriterOptions.Json)!;
        
        return Results.Ok(json);
    }
}
