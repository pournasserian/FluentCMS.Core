using FluentCMS.Core.Plugins.Abstractions;
using FluentCMS.Plugins.Authentication.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace FluentCMS.Plugins.Authentication;

public class AuthenticationPlugin : IPlugin
{
    public const string JWT_OPTIONS_SECTION_NAME = "JwtOptions";

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        var jwtSettingsSection = builder.Configuration.GetSection(JWT_OPTIONS_SECTION_NAME);
        if (!jwtSettingsSection.Exists())
            throw new InvalidOperationException("JwtSettings section is missing from appsettings.json");

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
            // Get JwtOptions from IOptions
            var serviceProvider = builder.Services.BuildServiceProvider();
            var jwtOptions = serviceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;

            var validationParams = jwtOptions.ValidationParameters;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = validationParams.ValidateIssuer,
                ValidateAudience = validationParams.ValidateAudience,
                ValidateLifetime = validationParams.ValidateLifetime,
                ValidateIssuerSigningKey = validationParams.ValidateIssuerSigningKey,
                RequireExpirationTime = validationParams.RequireExpirationTime,
                RequireSignedTokens = validationParams.RequireSignedTokens,
                ClockSkew = validationParams.ClockSkew,

                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(SHA512.HashData(Encoding.UTF8.GetBytes(jwtOptions.Secret)))
            };
        });

    }

    public void Configure(IApplicationBuilder app)
    {
    }
}
