using System.ComponentModel.DataAnnotations;

namespace FluentCMS.Plugins.Authentication.Options;

public class JwtOptions
{
    [Required(ErrorMessage = "JWT Secret is required")]
    public string Secret { get; set; } = default!;

    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; set; } = default!;

    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; set; } = default!;

    [Range(1, 30, ErrorMessage = "ExpirationInDays must be between 1 and 30 days")]
    public int ExpirationInDays { get; set; }

    public TokenValidationParametersOptions ValidationParameters { get; set; } = new();
}

public class TokenValidationParametersOptions
{
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
    public bool RequireExpirationTime { get; set; } = true;
    public bool RequireSignedTokens { get; set; } = true;
    public TimeSpan ClockSkew { get; set; } = TimeSpan.Zero;
}
