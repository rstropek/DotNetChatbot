# .NET 10 Chatbot Workshop

This is a comprehensive sample project for a conference workshop that demonstrates modern .NET development practices and cutting-edge features. The solution showcases a complete chatbot application built with .NET 10, integrating OpenAI API, Model Context Protocol (MCP), and .NET Aspire orchestration.

## What You'll Find Here

This workshop sample demonstrates:

- **OpenAI API Integration**: Working with OpenAI's latest APIs in .NET 10 for intelligent chat functionality
- **Modern .NET Features**: Utilizing the latest language features from .NET 9 and .NET 10
- **Server-Sent Events (SSE)**: New streaming implementation in .NET 10 for real-time chat experiences
- **Model Context Protocol (MCP)**: Both server and client implementations for extensible tool integration
- **Microservices Architecture**: Multiple services working together seamlessly
- **.NET Aspire Orchestration**: Modern cloud-native application orchestration and service discovery

## Project Structure

The solution consists of several interconnected components:

- **ChatBot**: Main chatbot API service with OpenAI integration and streaming responses
- **CartMcp**: MCP server providing shopping cart functionality as external tools
- **ChatBotDb**: Shared database layer with Entity Framework Core
- **ChatUI**: Modern web frontend for the chat interface
- **DotNetChatbot.AppHost**: Aspire orchestration host that ties everything together
- **DotNetChatbot.ServiceDefaults**: Shared service configuration and defaults

## The Demo Scenario

The chatbot acts as a friendly flower shop salesperson, helping customers choose bouquets and manage their shopping cart. This scenario demonstrates:

- Natural language processing with OpenAI
- Function calling and tool integration via MCP
- Real-time streaming responses
- Database persistence
- Modern web UI interactions

## Getting Started

This is a workshop sample designed to showcase .NET 10 capabilities. Each subfolder contains its own README with specific details about that component's role in the overall architecture.

Run the solution using .NET Aspire to see all components working together in a fully orchestrated environment.
