# .NET 10 Chatbot Workshop

This is a comprehensive sample project for a conference workshop that demonstrates modern .NET development practices and cutting-edge features. The solution showcases a complete chatbot application built with .NET 10, integrating OpenAI API, Model Context Protocol (MCP), and .NET Aspire orchestration.

## What You'll Find Here

This workshop sample demonstrates:

- **OpenAI API Integration**: Working with OpenAI's latest APIs in .NET 10 for intelligent chat functionality
- **Microsoft Agent Framework**: Side-by-side comparison of the traditional OpenAI SDK approach and the new Agent Framework that dramatically simplifies tool calling, streaming, and conversation management
- **Modern .NET Features**: Utilizing the latest language features from .NET 9 and .NET 10
- **Server-Sent Events (SSE)**: New streaming implementation in .NET 10 for real-time chat experiences
- **Model Context Protocol (MCP)**: Both server and client implementations for extensible tool integration
- **Microservices Architecture**: Multiple services working together seamlessly
- **.NET Aspire Orchestration**: Modern cloud-native application orchestration and service discovery

## Project Structure

The solution consists of several interconnected components:

- **ChatBot**: Main chatbot API service with two implementations — a traditional OpenAI SDK approach (`Traditional/`) and a Microsoft Agent Framework approach (`AgentFramework/`)
- **CartMcp**: MCP server providing shopping cart functionality as external tools
- **ChatBotDb**: Shared database layer with Entity Framework Core
- **ChatUI**: Modern web frontend for the chat interface
- **Tests**: Integration and unit tests using xUnit, covering database operations and web API endpoints
- **AppHost**: Aspire orchestration host that ties everything together
- **ServiceDefaults**: Shared service configuration and defaults

## The Demo Scenario

The chatbot acts as a friendly flower shop salesperson, helping customers choose bouquets and manage their shopping cart. This scenario demonstrates:

- Natural language processing with OpenAI
- Function calling and tool integration via MCP
- Real-time streaming responses
- Database persistence
- Web UI interactions
- Comparing a traditional, manual approach with the much simpler Agent Framework approach

## Getting Started

This is a workshop sample designed to showcase .NET 10 capabilities. Each subfolder contains its own README with specific details about that component's role in the overall architecture.

Run the solution using .NET Aspire to see all components working together in a fully orchestrated environment.

## What You Need to Participate

### IDE

During the workshop, we will be using [Visual Studio Code](https://code.visualstudio.com/) with the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension. You can use other IDEs (e.g. Visual Studio, Rider) if you prefer, but the instructions will be based on VS Code.

To raise developer productivity, it is highly recommended to install an AI coding assistant like [GitHub Copilot](https://github.com/features/copilot), [Kilo Code](https://kilocode.ai/), etc.

### Mandatory

As this is primarily a .NET 10 workshop, you will need:

* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
* [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) templates: `dotnet new install Aspire.ProjectTemplates`
* [.NET Entity Framework Core Tools (.NET 10)](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef --prerelease`
* [OpenAI API Key](https://platform.openai.com/settings/organization/api-keys) with <= €5 credit
  * Set it in the _AppHost_ project using: `dotnet user-secrets set "Parameters:openai-api-key" sk-proj-...`

### Optional

* If you want to also run the UI, you will need [Node.js](https://nodejs.org/en)
* If you want to run the SQLite web ui, you will need [Docker](https://www.docker.com/) or [Podman](https://podman.io/) (note the [required configuration for Podman](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?pivots=dotnet-cli#container-runtime))

## Code Analysis

The solution uses `AnalysisLevel=latest-all` for comprehensive code analysis. Several warnings are globally suppressed in `Directory.Build.props` because they are not applicable to ASP.NET Core applications:

| Warning | Reason for Suppression |
|---------|----------------------|
| **CA2007** (ConfigureAwait) | ASP.NET Core has no `SynchronizationContext`, so `ConfigureAwait(false)` is unnecessary and adds noise without benefit. |
| **CA1515** (Types should be internal) | ASP.NET Core requires many types to be public for minimal API endpoints, model binding, DI registration, and framework conventions. |
| **CA1062** (Validate parameters for null) | Parameters in ASP.NET Core endpoints and services are injected by the DI container or model binder, which guarantees they are non-null. |
| **CA1849** (Use async alternative) | `app.Run()` is the standard pattern in ASP.NET Core application templates; `RunAsync()` is not required in `Program.cs`. |
| **CA1034** (Do not nest types) | C# 14 extension blocks trigger this warning as a false positive since extension members appear as nested types to the analyzer. |
| **CA1708** (Names should differ by more than case) | C# 14 extension blocks on different types in the same class trigger this warning as a false positive. |

Additionally, the **Tests** project suppresses these warnings in its `.csproj`:

| Warning | Reason for Suppression |
|---------|----------------------|
| **CA1707** (Underscores in names) | Test methods use `Method_Scenario_Expected` naming convention, which is standard practice. |
| **CA2007** (ConfigureAwait) | xUnit manages the synchronization context; `ConfigureAwait` is unnecessary in tests. |
| **CA1515** (Types should be internal) | xUnit requires test classes and fixtures to be public. |

## Important Links

* **[Live View of Rainer's Code](https://dev-01.rstropek.com/files/) during the workshop** (Code `BASTA`)
* [.NET 10 What's New](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
* [Official .NET Library for OpenAI](https://github.com/openai/openai-dotnet)
* [OpenAI API Reference](https://platform.openai.com/docs/api-reference/introduction)
* [Microsoft Agent Framework](https://github.com/microsoft/agent-framework)
* [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/ai/ai-extensions)
* [Model Context Protocol (MCP) Specification](https://modelcontextprotocol.io/docs/getting-started/intro)
* [Official C# SDK for MCP](https://github.com/modelcontextprotocol/csharp-sdk)
* [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
