using FluentCMS.DataSeeder.Abstractions;

namespace FluentCMS.TodoApi.Repositories;

public class TodoSeeder : ISeeder
{
    public int Order => 100;

    public async Task Seed()
    {
        
    }
}
