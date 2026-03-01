using ChatBotDb;
using Microsoft.EntityFrameworkCore;

namespace ChatBot;

public static class MigrationExtensions
{
    extension(IServiceProvider serviceProvider)
    {
        public async Task ApplyMigrations()
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDataContext>();
            await context.Database.MigrateAsync();
        }
    }
}
