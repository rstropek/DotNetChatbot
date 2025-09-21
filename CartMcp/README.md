# CartMcp - Model Context Protocol Server

This service demonstrates a Model Context Protocol (MCP) server implementation that provides shopping cart functionality as external tools for the chatbot. It showcases how to build extensible AI systems using the MCP standard.

## What This Service Does

The CartMcp service acts as an MCP server that:

- **Provides External Tools**: Exposes shopping cart functionality that the chatbot can use through function calling
- **Handles Cart Operations**: Manages adding flower bouquets to a customer's shopping cart
- **Integrates with Database**: Persists orders using Entity Framework Core and the shared database
- **Follows MCP Standards**: Implements the Model Context Protocol for seamless tool integration

## Key Features Demonstrated

- **MCP Server Implementation**: Shows how to build a compliant MCP server in .NET 10
- **HTTP Transport**: Uses HTTP-based transport for MCP communication
- **Automatic Tool Discovery**: Tools are automatically discovered from assembly attributes
- **Database Integration**: Shares the same database context as other services
- **Aspire Integration**: Runs as part of the orchestrated Aspire application

## The Shopping Cart Tool

The service provides a single tool called `AddToCart` that:

1. Accepts flower bouquet details (flower type, color, size)
2. Creates an order record in the database
3. Returns a confirmation with the cart ID
4. Allows the chatbot to help customers build their orders

## Technical Implementation

This MCP server demonstrates:

- **Attribute-Based Tool Definition**: Uses `[McpServerTool]` attributes to define available functions
- **Dependency Injection**: Integrates with .NET's DI container for database access
- **Record Types**: Modern C# record syntax for clean data transfer objects
- **Component Descriptions**: Provides clear descriptions for AI function calling
- **Shared Infrastructure**: Leverages the same service defaults and database as other components

## MCP Protocol Benefits

By implementing MCP, this service shows how to:

- Create modular, reusable AI tools
- Enable dynamic tool discovery
- Maintain separation of concerns between AI logic and business functionality
- Build extensible systems where new capabilities can be added independently

This service is a practical example of how the Model Context Protocol enables building composable AI systems with .NET 10.
