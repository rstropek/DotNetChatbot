# ChatBot Service

This is the main chatbot API service that powers the intelligent conversation experience. It serves as the core backend that integrates OpenAI's language models with the Model Context Protocol (MCP) for extensible functionality.

## What This Service Does

The ChatBot service acts as a flower shop salesperson, providing an intelligent conversational interface that:

- **Processes Natural Language**: Uses OpenAI's GPT models to understand customer requests and provide helpful responses
- **Streams Real-time Responses**: Leverages .NET 10's new Server-Sent Events (SSE) implementation for smooth, real-time chat experiences
- **Integrates External Tools**: Connects to MCP servers to access additional functionality like shopping cart management
- **Maintains Conversation History**: Persists chat conversations in a database for context and continuity

## Key Features Demonstrated

- **OpenAI API Integration**: Modern .NET 10 patterns for working with OpenAI's streaming APIs
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

## Technical Highlights

This service showcases several .NET 10 and modern development features:

- Extension methods for endpoint mapping
- Minimal APIs with typed results
- Background service integration
- CORS configuration for web client support
- Configuration-based OpenAI client setup
- Activity source integration for distributed tracing

The service is designed to be a comprehensive example of building intelligent, streaming APIs with the latest .NET technologies.
