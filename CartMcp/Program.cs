using System.ComponentModel;
using ChatBotDb;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// Our MCP server runs as part of our Aspire project.
// Therefore, it gets shared configuration including OTel.
builder.AddServiceDefaults();

// Add our EF DbContext to the DI container.
builder.AddSqliteDbContext<ApplicationDataContext>("chatbot-db");

// Add the MCP server and configure it to use HTTP transport.
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapMcp();

app.Run();

[McpServerToolType]
public sealed class CartTool()
{
    [McpServerTool, Description("Stores a flower bouquet in the shopping cart")]
    public static string AddToCart(CartItem item, ApplicationDataContext context)
    {
        var order = new Order
        {
            Flower = item.Flower,
            Color = item.Color,
            Size = item.Size
        };
        context.Orders.Add(order);
        context.SaveChanges();

        // Note that we can return whatever we want. Could be a complex object, too.
        return $"Added to cart. The Cart ID is {order.Id}.";
    }

    public record CartItem(string Flower, string Color, string Size);
}
