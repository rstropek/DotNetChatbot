# ServiceDefaults - Shared Service Configuration

This project provides common service configuration and infrastructure setup that is shared across all services in the solution. It demonstrates .NET Aspire service defaults and modern observability patterns that every service should have.

The OpenTelemetry tracing configuration includes sources for both the traditional implementation and the Agent Framework (`ChatBot.AgentFramework`, `Microsoft.Extensions.AI`, `Microsoft.Agents`), enabling side-by-side trace comparison in the Aspire Dashboard.
