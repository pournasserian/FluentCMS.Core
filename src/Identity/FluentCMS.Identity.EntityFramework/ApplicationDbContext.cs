﻿namespace FluentCMS.Identity.EntityFramework;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
    IdentityDbContext<User, Role, Guid, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>(options),
    IEventPublisherDbContext
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Change Identity table names
        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
        });

        builder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
        });

        builder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<UserClaim>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<UserLogin>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<RoleClaim>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        builder.Entity<UserToken>(entity =>
        {
            entity.ToTable("UserTokens");
        });
    }
}
