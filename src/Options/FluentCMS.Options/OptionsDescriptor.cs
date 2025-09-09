namespace FluentCMS.Options;

public sealed record OptionsDescriptor(
    string Alias,           // unique alias for this options type, e.g. "identity", "my"
    string? ConfigSection,  // optional appsettings section to bind defaults from
    Type Type,              // typeof(IdentityOptions), typeof(MyOptions), ...
    object DefaultValue     // default value if found in appsettings, or empty instance
);