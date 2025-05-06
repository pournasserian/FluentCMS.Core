using FluentCMS.DataAccess.Abstractions;

namespace FluentCMS.DataAccess.EntityFramework;

public class RepositoryRegistry
{
    public readonly Dictionary<Type, Type> CustomRepositoryTypes = [];
    public readonly Dictionary<Type, Type> CustomRepositoryImplementations = [];

    public RepositoryRegistry()
    {
        ScanAssemblies();
    }

    private void ScanAssemblies()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var genericRepositoryType = typeof(IRepository<>);

        // Find all repository interfaces
        var customRepositoryInterfacesTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsInterface && !t.IsGenericType && t.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericRepositoryType));

        foreach (var interfaceType in customRepositoryInterfacesTypes)
        {
            // Get the entity type from the IRepository<T> interface
            var entityType = interfaceType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericRepositoryType)
                .GetGenericArguments()[0];

            // Store the mapping
            CustomRepositoryTypes[entityType] = interfaceType;

            // Find corresponding implementation
            var implementationType = assemblies
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => !t.IsInterface && !t.IsAbstract && interfaceType.IsAssignableFrom(t));

            if (implementationType != null)
            {
                CustomRepositoryImplementations[interfaceType] = implementationType;
            }
        }
    }

    public Type? GetRepositoryInterfaceType(Type entityType)
    {
        return CustomRepositoryTypes.TryGetValue(entityType, out var interfaceType)
            ? interfaceType
            : null;
    }

    public Type? GetRepositoryImplementationType(Type interfaceType)
    {
        return CustomRepositoryImplementations.TryGetValue(interfaceType, out var implementationType)
            ? implementationType
            : null;
    }
}
