using FluentCMS.Core.Plugins.Abstractions;
using FluentCMS.TodoApi.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.TodoApi;

public class TodoPlugin : IPlugin
{
    public string Name => "Todo API Plugin";
    public string Version => "1.0.0";
    public string Description => "Provides Todo functionality with CRUD operations";
    public bool IsEnabled { get; set; } = true;

    public bool Initialize(IServiceCollection services)
    {
        // Register the service
        services.AddScoped<ITodoService, TodoService>();
        
        return true;
    }

    public Task<bool> Start()
    {
        // No special startup needed
        return Task.FromResult(true);
    }

    public Task<bool> Stop()
    {
        // No special cleanup needed
        return Task.FromResult(true);
    }
}
