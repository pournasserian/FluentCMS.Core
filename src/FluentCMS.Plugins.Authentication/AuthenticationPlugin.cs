using FluentCMS.Core.Identity.Models;
using FluentCMS.Core.Identity.Options;
using FluentCMS.Core.Plugins.Abstractions;
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
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        //builder.Services.AddIdentityCore<User>()
        //    .AddUserStore<UserStore>()
        //    .AddRoleStore<RoleStore>()
        //    .AddUserManager<UserManager<User>>()
        //    .AddRoleManager<RoleManager<Role>>()
        //    .AddDefaultTokenProviders();

        // Configure Identity options from appsettings.json
        builder.Services.Configure<IdentityOptions>(options =>
            builder.Configuration.GetSection("IdentityOptions").Bind(options));

        // Optionally, access specific sections directly if needed
        builder.Services.Configure<PasswordOptions>(options =>
            builder.Configuration.GetSection("IdentityOptions:Password").Bind(options));
        builder.Services.Configure<LockoutOptions>(options =>
            builder.Configuration.GetSection("IdentityOptions:Lockout").Bind(options));
        builder.Services.Configure<UserOptions>(options =>
            builder.Configuration.GetSection("IdentityOptions:User").Bind(options));

        var jwtSettingsSection = builder.Configuration.GetSection("JwtOptions");
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
