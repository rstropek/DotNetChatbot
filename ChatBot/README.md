# ChatBot Service

This is the main chatbot API service that powers the intelligent conversation experience. It serves as the core backend that integrates OpenAI's language models with the Model Context Protocol (MCP) for extensible functionality.

## Two Implementations Side by Side

The service contains **two implementations** of the same chatbot, allowing students to compare approaches:

| | Traditional (`/conversations/`) | Agent Framework (`/af/conversations/`) |
|-|---|----|
| **Folder** | `Traditional/` | `AgentFramework/` |
| **Tool dispatch** | Manual `do/while` loop detecting `FunctionCallResponseItem`, switching on function name, deserializing arguments, calling the function, storing results | Automatic — `RunStreamingAsync` handles the entire tool calling loop |
| **Tool definition** | `FunctionHelpers.ToJsonSchema<T>()` + `ResponseTool.CreateFunctionTool(...)` + separate request record type | `[Description]` attribute on a static method + `AIFunctionFactory.Create()` |
| **MCP integration** | Manual conversion from `McpClientTool` to `FunctionTool`, manual `CallToolAsync` dispatch | `McpClientTool` implements `AITool` — pass directly to agent |
| **Conversation history** | Stored in SQLite via `ISessionRepository` as serialized `List<ResponseItem>`, loaded on each request | Managed in-memory by `AgentSession`, serialized to SQLite via `ISessionRepository` |
| **Streaming** | Manual `await foreach` over `StreamingResponseUpdate`, filtering for `StreamingResponseOutputTextDeltaUpdate` | `await foreach` over `AgentResponseUpdate`, reading `.Text` |
| **Observability** | Manual `ActivitySource` spans | `.UseOpenTelemetry()` one-liner on the agent builder |

Both implementations expose the **same SSE format** (`{ "deltaText": "..." }` with event type `textDelta`), so the frontend only needs to change the URL prefix.

## What This Service Does

The ChatBot service acts as a flower shop salesperson, providing an intelligent conversational interface that:

- **Processes Natural Language**: Uses OpenAI's GPT models to understand customer requests and provide helpful responses
- **Streams Real-time Responses**: Leverages .NET 10's new Server-Sent Events (SSE) implementation for smooth, real-time chat experiences
- **Integrates External Tools**: Connects to MCP servers to access additional functionality like shopping cart management
- **Maintains Conversation History**: Persists chat conversations as session blobs in SQLite via a unified `ISessionRepository` (used by both implementations)

## Project Structure

```
ChatBot/
  Program.cs                         # Registers both implementations, maps both endpoint groups
  McpToolsProvider.cs                # Shared MCP client — provides tools for both implementations
  DeveloperMessageProvider.cs        # Loads and caches the system prompt from embedded resource
  MigrationManager.cs               # Shared DB migration logic
  developer-message.md              # Shared system prompt (embedded resource)
  Traditional/
    OpenAI.cs                        # OpenAIManager — manual streaming + tool dispatch
    Conversations.cs                 # Endpoints at /conversations/...
    ProductsTools.cs                 # FunctionTool definition with manual JSON schema
    FunctionHelpers.cs               # JSON schema generation utility
    McpToolsExtensions.cs            # Bridges McpClientTool to OpenAI FunctionTool
  AgentFramework/
    AgentManager.cs                  # AIAgent-based implementation with automatic tool handling
    Conversations.cs                 # Endpoints at /af/conversations/...
    McpToolsExtensions.cs            # Bridges McpToolsProvider to AITool (simple cast)
    README.md                        # Detailed comparison of the two approaches
```

## Key Features Demonstrated

- **OpenAI API Integration**: Modern .NET 10 patterns for working with OpenAI's streaming APIs
- **Microsoft Agent Framework**: Simplified agent construction with automatic tool dispatch and session management
- **Function Calling**: Dynamic tool discovery and execution through the Model Context Protocol
- **Async Streaming**: Real-time response streaming using `IAsyncEnumerable` and SSE
- **Entity Framework Core**: Database operations with SQLite for conversation persistence
- **Dependency Injection**: Clean architecture with scoped services and proper separation of concerns
- **Observability**: Built-in telemetry and activity tracking with OpenTelemetry

## The Conversation Flow

1. Users send messages through the chat interface
2. The service adds user messages to the conversation history
3. OpenAI processes the conversation with available MCP tools
4. Responses are streamed back in real-time
5. Tool calls (like adding items to cart) are executed automatically
6. All interactions are persisted for future reference

## NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `OpenAI` | 2.8.0 | Direct OpenAI SDK — pinned to 2.8.0 to match the version `Microsoft.Extensions.AI.OpenAI` was compiled against (see [agent-framework#4380](https://github.com/microsoft/agent-framework/issues/4380)) |
| `ModelContextProtocol` | 1.0.0 | MCP client for tool integration |
| `Microsoft.Agents.AI.OpenAI` | 1.0.0-rc2 | Agent Framework with OpenAI support |
| `Microsoft.Extensions.AI.OpenAI` | 10.3.0 | `IChatClient` abstraction over OpenAI |
