# ChatBotDb - Database Layer

This project provides the shared database layer for the entire chatbot solution. It demonstrates modern Entity Framework Core practices and serves as the data persistence foundation for all services.

## What This Project Does

The ChatBotDb project:

- **Defines Data Models**: Contains all entity classes that represent the application's data structure
- **Provides Database Context**: Implements the Entity Framework Core `DbContext` for database operations
- **Manages Migrations**: Handles database schema creation and updates through EF Core migrations
- **Shared Repository**: Offers a common data access layer used by multiple services

## Key Components

### Data Models
- **Conversation**: Represents chat conversations with unique identifiers
- **ResponseItem**: Stores individual messages and responses within conversations
- **Order**: Represents flower bouquet orders placed through the shopping cart

### Database Context
- **ApplicationDataContext**: The main EF Core context that manages all entities
- **Repository Pattern**: Implements `IConversationRepository` for clean data access abstraction

### Migration Management
- **Initial Migration**: Sets up the base database schema
- **Migration Manager**: Utility for applying database migrations at startup
