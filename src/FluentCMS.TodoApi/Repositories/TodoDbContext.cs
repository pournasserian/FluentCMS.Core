using FluentCMS.TodoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FluentCMS.TodoApi.Repositories;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<Todo> Todos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure the Todo entity
        modelBuilder.Entity<Todo>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<Todo>()
            .Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(150);

        modelBuilder.Entity<Todo>()
            .Property(t => t.Description)
            .HasMaxLength(500);

        // Seed initial data
        modelBuilder.Entity<Todo>().HasData(
            new Todo
            {
                Id = Guid.NewGuid(),
                Title = "Complete EF Core tutorial",
                Description = "Finish the Entity Framework Core getting started tutorial",
                IsCompleted = false,
                DueDate = DateTime.Now.AddDays(3),
                CreatedAt = DateTime.Now,
                Version = 1
            },
            new Todo
            {
                Id = Guid.NewGuid(),
                Title = "Buy groceries",
                Description = "Milk, eggs, bread, and vegetables",
                IsCompleted = false,
                DueDate = DateTime.Now.AddDays(1),
                CreatedAt = DateTime.Now,
                Version = 1
            }
        );
    }

}