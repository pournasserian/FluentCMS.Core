using FluentCMS.Core.Api;
using FluentCMS.Core.Repositories.LiteDB;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddFluentCmsApi();

builder.Services.AddLiteDBRepositories(builder.Configuration);

var app = builder.Build();

app.UseFluentCmsApi();

app.Run();
