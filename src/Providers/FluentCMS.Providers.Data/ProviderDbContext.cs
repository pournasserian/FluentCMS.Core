namespace FluentCMS.Providers.Data;

/// <summary>
/// Database context for the provider system.
/// </summary>
public class ProviderDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public ProviderDbContext(DbContextOptions<ProviderDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the provider types.
    /// </summary>
    public DbSet<ProviderType> ProviderTypes { get; set; } = null!;

    /// <summary>
    /// Gets or sets the provider implementations.
    /// </summary>
    public DbSet<ProviderImplementation> ProviderImplementations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the provider configurations.
    /// </summary>
    public DbSet<ProviderConfiguration> ProviderConfigurations { get; set; } = null!;

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ProviderType
        modelBuilder.Entity<ProviderType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FullTypeName).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FullTypeName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AssemblyName).IsRequired().HasMaxLength(200);
        });

        // Configure ProviderImplementation
        modelBuilder.Entity<ProviderImplementation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProviderTypeId, e.FullTypeName }).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(20);
            entity.Property(e => e.FullTypeName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AssemblyPath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.HealthMessage).HasMaxLength(1000);

            // Configure relationship to ProviderType
            entity.HasOne(e => e.ProviderType)
                .WithMany()
                .HasForeignKey(e => e.ProviderTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ProviderConfiguration
        modelBuilder.Entity<ProviderConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ImplementationId).IsUnique();
            entity.Property(e => e.ConfigurationJson).IsRequired();

            // Configure relationship to ProviderImplementation
            entity.HasOne(e => e.Implementation)
                .WithMany()
                .HasForeignKey(e => e.ImplementationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
