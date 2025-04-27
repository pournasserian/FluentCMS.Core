using FluentCMS.Core.Plugins.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Plugins.Authentication;

public class AuthenticationPlugin : IPlugin
{

    public IServiceCollection ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });
        //.AddJwtBearer(options =>
        //{
        //    var serviceProvider = services.BuildServiceProvider();
        //    var jwtOptions = serviceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;

        //    options.TokenValidationParameters = new TokenValidationParameters
        //    {
        //        ValidateIssuer = jwtOptions.ValidationParameters.ValidateIssuer,
        //        ValidateAudience = jwtOptions.ValidationParameters.ValidateAudience,
        //        ValidateLifetime = jwtOptions.ValidationParameters.ValidateLifetime,
        //        ValidateIssuerSigningKey = jwtOptions.ValidationParameters.ValidateIssuerSigningKey,
        //        RequireExpirationTime = jwtOptions.ValidationParameters.RequireExpirationTime,
        //        RequireSignedTokens = jwtOptions.ValidationParameters.RequireSignedTokens,
        //        ClockSkew = jwtOptions.ValidationParameters.ClockSkew,

        //        ValidIssuer = jwtOptions.Issuer,
        //        ValidAudience = jwtOptions.Audience,
        //        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
        //    };
        //});
        return services;

    }

    public IApplicationBuilder Configure(IApplicationBuilder app)
    {
        return app;
    }
}
