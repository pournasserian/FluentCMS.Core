using System.Reflection;
using FluentCMS.Core.Interception.Abstractions;
using FluentCMS.Core.Repositories.Abstractions;

namespace FluentCMS.Core.Interception.Interceptors.HistoryTracking;

/// <summary>
/// Interceptor that tracks changes to entities in the repository.
/// </summary>
public class EntityHistoryInterceptor : IMethodInterceptor
{
    private readonly IHistoryRecorder _historyRecorder;
    private readonly IUserContextAccessor _userContext;
    
    /// <summary>
    /// Gets the order in which this interceptor should be executed relative to other interceptors.
    /// This interceptor should run before other interceptors to ensure history is recorded accurately.
    /// </summary>
    public int Order => 10;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityHistoryInterceptor"/> class.
    /// </summary>
    /// <param name="historyRecorder">The history recorder to use.</param>
    /// <param name="userContext">The user context accessor to use.</param>
    public EntityHistoryInterceptor(
        IHistoryRecorder historyRecorder,
        IUserContextAccessor userContext)
    {
        _historyRecorder = historyRecorder ?? throw new ArgumentNullException(nameof(historyRecorder));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }
    
    /// <inheritdoc />
    public void BeforeExecute(MethodExecutionContext context)
    {
        // Only intercept repository methods
        if (!IsRepositoryMethod(context))
            return;
            
        // For update/delete methods, we need to capture the current state
        if (IsUpdateMethod(context) || IsDeleteMethod(context))
        {
            var entityId = GetEntityId(context);
            if (entityId != Guid.Empty)
            {
                // Use the GetById method to retrieve the current entity state
                var currentEntity = GetCurrentEntityState(context, entityId);
                
                if (currentEntity != null)
                {
                    // Store the entity type, which will be used to create the appropriate EntityHistory<T>
                    var entityType = GetEntityTypeFromRepository(context);
                    if (entityType != null)
                    {
                        // Save the information for the AfterExecute method
                        context.Items["EntityHistoryInterceptor_Entity"] = currentEntity;
                        context.Items["EntityHistoryInterceptor_EntityType"] = entityType;
                        context.Items["EntityHistoryInterceptor_Action"] = IsUpdateMethod(context) ? "Update" : "Delete";
                    }
                }
            }
        }
    }
    
    /// <inheritdoc />
    public void AfterExecute(MethodExecutionContext context)
    {
        // Only intercept repository methods
        if (!IsRepositoryMethod(context))
            return;
            
        try
        {
            // Handle Add operations
            if (IsAddMethod(context) && context.Result != null)
            {
                // For Add operations, we record history after the entity has been added
                var entity = context.Result;
                if (entity is IBaseEntity baseEntity)
                {
                    var entityType = entity.GetType();
                    // Use reflection to call the generic RecordHistory method
                    RecordHistoryGeneric(entityType, baseEntity, "Create", _userContext.GetCurrentUsername());
                }
            }
            // Handle Update and Delete operations where we captured the entity in BeforeExecute
            else if (context.Items.TryGetValue("EntityHistoryInterceptor_Entity", out var entity) &&
                     context.Items.TryGetValue("EntityHistoryInterceptor_EntityType", out var entityTypeObj) &&
                     context.Items.TryGetValue("EntityHistoryInterceptor_Action", out var actionObj))
            {
                var action = actionObj.ToString() ?? "";
                var entityType = entityTypeObj as Type;
                
                if (entity != null && entityType != null)
                {
                    // Use reflection to call the generic RecordHistory method
                    RecordHistoryGeneric(entityType, entity, action, _userContext.GetCurrentUsername());
                }
            }
        }
        catch (Exception)
        {
            // Log exception but don't rethrow - we don't want history tracking to break normal operation
        }
    }
    
    /// <inheritdoc />
    public void OnException(MethodExecutionContext context)
    {
        // Optionally record failed operations
    }
    
    #region Helper Methods
    
    private bool IsRepositoryMethod(MethodExecutionContext context)
    {
        return context.TargetType.IsAssignableTo(typeof(IBaseEntityRepository<>).MakeGenericType(GetEntityTypeFromRepository(context) ?? typeof(IBaseEntity)));
    }
    
    private bool IsAddMethod(MethodExecutionContext context)
    {
        return context.Method.Name == "Add";
    }
    
    private bool IsUpdateMethod(MethodExecutionContext context)
    {
        return context.Method.Name == "Update";
    }
    
    private bool IsDeleteMethod(MethodExecutionContext context)
    {
        return context.Method.Name == "Remove";
    }
    
    private Guid GetEntityId(MethodExecutionContext context)
    {
        if (IsUpdateMethod(context) && context.Arguments.Length > 0 && context.Arguments[0] is IBaseEntity entity)
        {
            return entity.Id;
        }
        
        if (IsDeleteMethod(context) && context.Arguments.Length > 0 && context.Arguments[0] is Guid id)
        {
            return id;
        }
        
        return Guid.Empty;
    }
    
    private Type? GetEntityTypeFromRepository(MethodExecutionContext context)
    {
        // Try to get the entity type from the repository's generic type argument
        foreach (var interfaceType in context.TargetType.GetInterfaces())
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IBaseEntityRepository<>))
            {
                return interfaceType.GetGenericArguments()[0];
            }
        }
        
        return null;
    }
    
    private object? GetCurrentEntityState(MethodExecutionContext context, Guid id)
    {
        try
        {
            // Find the GetById method on the repository
            var getByIdMethod = context.TargetType.GetMethod("GetById", 
                BindingFlags.Public | BindingFlags.Instance);
                
            if (getByIdMethod != null)
            {
                // Call the GetById method to get the current entity
                var task = getByIdMethod.Invoke(context.TargetInstance, new object[] { id, CancellationToken.None });
                if (task != null)
                {
                    // Get the result from the task
                    var taskType = task.GetType();
                    var resultProperty = taskType.GetProperty("Result");
                    
                    if (resultProperty != null)
                    {
                        return resultProperty.GetValue(task);
                    }
                }
            }
        }
        catch
        {
            // Ignore errors, we don't want to break the main operation
        }
        
        return null;
    }
    
    private void RecordHistoryGeneric(Type entityType, object entity, string action, string username)
    {
        // Get the generic method
        var methodInfo = typeof(IHistoryRecorder).GetMethod("RecordHistory");
        if (methodInfo != null)
        {
            // Make it generic with the entity type
            var genericMethod = methodInfo.MakeGenericMethod(entityType);
            
            // Invoke it
            var task = genericMethod.Invoke(_historyRecorder, new[] { entity, action, username });
            
            // If we want to wait for it to complete, we could, but that might slow down operations
            // For now, we'll let it run asynchronously
        }
    }
    
    #endregion
}
