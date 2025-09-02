using FluentCMS.Providers.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace FluentCMS.Providers;

public interface IProviderManager
{
    void ConfigureServices(IHostApplicationBuilder builder);
    void Configure(IApplicationBuilder app);
}