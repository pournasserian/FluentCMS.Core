namespace FluentCMS.Options.EntityFramework;

// DbContext for accessing the configuration database
public class ConfigOptionsDbContext(DbContextOptions<ConfigOptionsDbContext> options) : BaseDbContext(options)
{
    public DbSet<ConfigOptions> ConfigOptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ConfigOptions>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<ConfigOptions>()
            .Property(t => t.TypeName)
            .IsRequired();

        modelBuilder.Entity<ConfigOptions>()
            .Property(t => t.Value)
            .IsRequired();
    }

    public void InitializeDb() 
    {
        if (!Database.CanConnect())
            Database.CanConnect();

        try
        {
            // Check if tables exist, if not, create them
            var script = Database.GenerateCreateScript();
            Database.ExecuteSqlRaw(script);
        }
        catch (Exception)
        {
        }
    }
}