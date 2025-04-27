using FluentCMS.Core.Plugins.Abstractions;
using FluentCMS.Plugins.Authentication.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FluentCMS.Plugins.Authentication;

public class AuthenticationPlugin : IPlugin
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
        if (!jwtSettingsSection.Exists())
        {
            throw new InvalidOperationException("JwtSettings section is missing from appsettings.json");
        }

        builder.Services.Configure<JwtOptions>(jwtSettingsSection);

        // Validate JwtOptions after binding
        builder.Services.AddOptions<JwtOptions>()
            .Bind(jwtSettingsSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            var jwtOptions = serviceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = jwtOptions.ValidationParameters.ValidateIssuer,
                ValidateAudience = jwtOptions.ValidationParameters.ValidateAudience,
                ValidateLifetime = jwtOptions.ValidationParameters.ValidateLifetime,
                ValidateIssuerSigningKey = jwtOptions.ValidationParameters.ValidateIssuerSigningKey,
                RequireExpirationTime = jwtOptions.ValidationParameters.RequireExpirationTime,
                RequireSignedTokens = jwtOptions.ValidationParameters.RequireSignedTokens,
                ClockSkew = jwtOptions.ValidationParameters.ClockSkew,

                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
            };
        });

    }

    public void Configure(IApplicationBuilder app)
    {
    }
}
