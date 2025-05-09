namespace FluentCMS.Plugins.Authentication;

public class AuthenticationPlugin : IPlugin
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
    }

    public void Configure(IApplicationBuilder app)
    {
        // Initialize the database in development environment only
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var dbContext = sp.GetRequiredService<ApplicationDbContext>();

            // Add this check to avoid conflicts
            if (!dbContext.Database.CanConnect())
            {
                dbContext.Database.EnsureCreated();
            }
            else
            {
                // For subsequent DbContexts, we need a different approach
                // This will ensure the tables for this specific DbContext are created
                // without dropping existing tables
                var script = dbContext.Database.GenerateCreateScript();
                dbContext.Database.ExecuteSqlRaw(script);
            }
        }
    }
}
