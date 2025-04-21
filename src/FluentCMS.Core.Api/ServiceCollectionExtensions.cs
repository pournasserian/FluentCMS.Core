using FluentCMS.Core.Api.Filters;
using FluentCMS.Core.Api.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace FluentCMS.Core.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFluentCmsApi(this IServiceCollection services)
    {
        //services.AddApplicationServices();
        services.AddOptions<JwtOptions>()
            .BindConfiguration("JwtOptions");

        services
            .AddControllers(config =>
            {
                config.Filters.Add<ApiResultActionFilter>();
                config.Filters.Add<ApiResultExceptionFilter>();
            })
            .AddJsonOptions(options =>
            {
                //options.JsonSerializerOptions.Converters.Add(new DictionaryJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = (context) =>
                {
                    var apiExecutionContext = services.BuildServiceProvider().GetRequiredService<IApplicationExecutionContext>();
                    var apiResult = new ApiResult<object>
                    {
                        Duration = (DateTime.UtcNow - apiExecutionContext.StartDate).TotalMilliseconds,
                        SessionId = apiExecutionContext.SessionId,
                        TraceId = apiExecutionContext.TraceId,
                        UniqueId = apiExecutionContext.UniqueId,
                        Status = 400,
                        IsSuccess = false
                    };

                    foreach (var item in context.ModelState)
                    {
                        var errors = item.Value.Errors;
                        if (errors?.Count > 0)
                        {
                            foreach (var error in errors)
                            {
                                apiResult.Errors.Add(new ApiError { Code = item.Key, Description = error.ErrorMessage });
                            }
                        }
                    }

                    return new BadRequestObjectResult(apiResult);
                };
            });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer((c) =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var options = serviceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
                var key = SHA512.Create().ComputeHash(Encoding.UTF8.GetBytes(options.Secret));
                c.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    SaveSigninToken = true,
                    ValidAudience = options.Audience,
                    ValidIssuer = options.Issuer
                };
            });

        services.AddAuthorization();

        services.AddHttpContextAccessor();

        services.AddScoped<IApplicationExecutionContext>(sp => new ApplicationExecutionContext(sp.GetRequiredService<IHttpContextAccessor>()));

        //services.AddAutoMapper(typeof(ApiServiceExtensions));

        services.AddApiDocumentation();
        return services;
    }

    public static WebApplication UseFluentCmsApi(this WebApplication app)
    {
        app.UseApiDocumentation();

        app.UseAuthentication();

        app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), app =>
        {
            // this will be executed only when the path starts with "/api"
            app.UseMiddleware<JwtAuthorizationMiddleware>();
        });

        app.UseAuthorization();

        app.MapControllers();

        return app;
    }

    private static string _applicationName = string.Empty;
    private static string _applicationVersion = string.Empty;

    private static IServiceCollection AddApiDocumentation(this IServiceCollection services, string applicationName = "FluentCMS API", string version = "v1.0.0")
    {
        _applicationName = applicationName;
        _applicationVersion = version;

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = applicationName, Version = version });

            // Define the security scheme for bearer tokens
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.OrderActionsBy((apiDesc) => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}");
        });

        return services;
    }

    private static IApplicationBuilder UseApiDocumentation(this IApplicationBuilder app)
    {
        // Enable middleware to serve generated Swagger as a JSON endpoint
        app.UseSwagger();

        // Enable middleware to serve Swagger UI
        app.UseSwaggerUI(c =>
        {
            c.DisplayRequestDuration();
            c.SwaggerEndpoint("/swagger/v1/swagger.json", _applicationName + " " + _applicationVersion);
            c.RoutePrefix = "api/doc";
            c.DocExpansion(DocExpansion.None);
        });

        return app;
    }
}
