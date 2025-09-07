using FluentCMS.DataSeeder.Abstractions;
using FluentCMS.TodoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.TodoApi.Repositories;

public class TodoSeeder(TodoDbContext dbContext, IDatabaseManager databaseManager) : ISeeder
{
    public int Order => 1000;

    public Task<bool> ShouldCreateSchema(CancellationToken cancellationToken = default)
    {
        return databaseManager.DatabaseExists(cancellationToken);
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
