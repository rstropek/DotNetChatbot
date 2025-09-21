using System.Collections.ObjectModel;

namespace ChatBotDb;

public class Conversation
{
    public int Id { get; set; }
    public Collection<Message> Messages { get; set; } = [];
}

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public string Content { get; set; } = "";
}

public class Order
{
    public int Id { get; set; }
    public string Flower { get; set; } = "";
    public string Color { get; set; } = "";
    public string Size { get; set; } = "";
}