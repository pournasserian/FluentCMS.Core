namespace FluentCMS.Identity.EntityFramework;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddIdentityCore(IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddCoreDbContext<ApplicationDbContext>();

        services.AddIdentity<User, Role>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

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

        return builder;
    }
}
