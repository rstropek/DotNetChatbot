using ChatBotDb;
using Microsoft.EntityFrameworkCore;

namespace ChatBot;

public class MigrationManager(ApplicationDataContext context, ILogger<MigrationManager> logger)
{
    public async Task ApplyMigrationsAsync()
    {
        logger.LogInformation("Applying migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Migrations applied.");
    }
}

public static class MigrationsManagerExtensions
{
    extension(IServiceProvider serviceProvider)
    {
        public async Task ApplyMigrations()
        {
            using var scope = serviceProvider.CreateScope();
            var migrationManager = scope.ServiceProvider.GetRequiredService<MigrationManager>();
            await migrationManager.ApplyMigrationsAsync();
        }
    }
}
