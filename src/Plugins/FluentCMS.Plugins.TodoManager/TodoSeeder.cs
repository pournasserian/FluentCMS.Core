using FluentCMS.DataSeeder.Abstractions;
using FluentCMS.Plugins.TodoManagement.Models;
using FluentCMS.Plugins.TodoManager.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.Plugins.TodoManager;

public class TodoSeeder(TodoDbContext dbContext, IDatabaseManager databaseManager) : ISeeder
{
    public int Order => 10000;

    public async Task<bool> ShouldCreateSchema(CancellationToken cancellationToken = default)
    {
        if (!await databaseManager.DatabaseExists(cancellationToken))
            return true;
        return !await databaseManager.TablesExist(["Todos"], cancellationToken);
    }

    public async Task CreateSchema(CancellationToken cancellationToken = default)
    {
        await databaseManager.CreateDatabase(cancellationToken);
        var sql = dbContext.Database.GenerateCreateScript();
        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public async Task<bool> ShouldSeed(CancellationToken cancellationToken = default)
    {
        return await databaseManager.TablesEmpty(["Todos"], cancellationToken);
    }

    public async Task SeedData(CancellationToken cancellationToken = default)
    {
        await dbContext.Todos.AddRangeAsync([
            new Todo
            {
                Title = "Complete EF Core tutorial",
                Description = "Finish the Entity Framework Core getting started tutorial",
                IsCompleted = false,
                DueDate = DateTime.Now.AddDays(3)
            },
            new Todo
            {
                Title = "Buy groceries",
                Description = "Milk, eggs, bread, and vegetables",
                IsCompleted = false,
                DueDate = DateTime.Now.AddDays(1)
            }
        ], cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
