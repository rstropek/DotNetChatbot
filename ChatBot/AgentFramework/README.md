# Agent Framework Implementation

This folder contains the **Microsoft Agent Framework** implementation of the chatbot. It provides the same functionality as the traditional implementation in `Traditional/`, but with significantly less code thanks to the framework's built-in abstractions.

## Why Agent Framework?

The [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) provides high-level abstractions that eliminate most of the boilerplate needed when working directly with the OpenAI SDK. The framework handles the entire tool calling loop internally detecting function calls, invoking the right tool, feeding results back, and continuing until the model produces a final text response.

### Observability

**Traditional**: Manual `ActivitySource` creation and span management.

**Agent Framework**: One-liner via the agent builder:

```csharp
agent = builtAgent.AsBuilder()
    .UseOpenTelemetry(sourceName: "ChatBot.AgentFramework")
    .Build();
```

### Conversation History

Both implementations use the same `ISessionRepository` to persist session data as a single blob in `Conversation.SessionData`.

**Traditional**: Serializes `List<ResponseItem>` to/from JSON using `ModelReaderWriter`. The endpoint orchestrates load → deserialize → stream → serialize → save.

**Agent Framework**: Managed automatically by `AgentSession`. The session is serialized/deserialized via `AIAgent.SerializeSessionAsync` / `DeserializeSessionAsync`.
