using FluentCMS.Core.Api;
using FluentCMS.Core.Plugins;
using FluentCMS.Core.Repositories.LiteDB;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddFluentCmsApi();
//builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddLiteDBRepositories(builder.Configuration);

// Add plugin system
builder.Services.AddPlugins();

var app = builder.Build();

app.UseFluentCmsApi();

// Use plugin system
app.UsePlugins();

app.Run();
