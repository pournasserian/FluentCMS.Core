namespace FluentCMS.Identity.EntityFramework;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : 
    IdentityDbContext<User, Role, Guid, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>(options),
    IEventPublisherDbContext
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Set table names
        builder.Entity<User>(b => b.ToTable("Users"));
        builder.Entity<Role>(b => b.ToTable("Roles"));
        builder.Entity<UserClaim>(b => b.ToTable("UserClaims"));
        builder.Entity<UserRole>(b => b.ToTable("UserRoles"));
        builder.Entity<UserLogin>(b => b.ToTable("UserLogins"));
        builder.Entity<UserToken>(b => b.ToTable("UserTokens"));
        builder.Entity<RoleClaim>(b => b.ToTable("RoleClaims"));
        
        base.OnModelCreating(builder);
    }
}
