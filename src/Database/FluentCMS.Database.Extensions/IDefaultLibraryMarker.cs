using FluentCMS.Database.Abstractions;

namespace FluentCMS.Database.Extensions;

/// <summary>
/// Default library marker for services that should use the default database configuration.
/// 
/// This marker is automatically registered when SetDefault() is called in the database configuration.
/// Services that don't belong to a specific library or should use the default database can use this marker.
/// 
/// Example:
/// <code>
/// // Service using default database
/// public class GeneralService
/// {
///     public GeneralService(IDatabaseManager&lt;IDefaultLibraryMarker&gt; databaseManager) { }
/// }
/// 
/// // Configuration
/// services.AddDatabaseManager(options =>
/// {
///     options.SetDefault().UseSqlServer(defaultConnectionString);
///     // This automatically registers IDefaultLibraryMarker
/// });
/// </code>
/// </summary>
public interface IDefaultLibraryMarker : IDatabaseManagerMarker
{
    // Empty interface - serves purely as a marker for default database configuration
}
