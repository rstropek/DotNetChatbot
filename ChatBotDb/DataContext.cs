using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatBotDb;

public partial class ApplicationDataContext(DbContextOptions<ApplicationDataContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Order> Orders { get; set; }
}

public class ApplicationDataContextFactory : IDesignTimeDbContextFactory<ApplicationDataContext>
{
    public ApplicationDataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDataContext>();
        optionsBuilder.UseSqlite(string.Empty);
        return new ApplicationDataContext(optionsBuilder.Options);
    }
}