using FluentCMS.Core.EventBus.Abstractions;
using FluentCMS.Core.Identity.DocumentDbStores;
using FluentCMS.Core.Identity.Models;
using FluentCMS.Core.Repositories.LiteDB;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Core.Identity.LiteDB;

public class RoleRepository<TRole>(ILiteDBContext dbContext, ILogger<AuditableEntityRepository<TRole>> logger, IEventPublisher eventPublisher, IApplicationExecutionContext executionContext) : RoleRepository<TRole, RoleClaim>(dbContext, logger, eventPublisher, executionContext), IRoleRepository<TRole> where TRole : Role
{
}

public class RoleRepository<TRole, TRoleClaim>(ILiteDBContext dbContext, ILogger<AuditableEntityRepository<TRole>> logger, IEventPublisher eventPublisher, IApplicationExecutionContext executionContext) : AuditableEntityRepository<TRole>(dbContext, logger, eventPublisher, executionContext), IRoleRepository<TRole, TRoleClaim> where TRole : Role where TRoleClaim : RoleClaim, new()
{
}