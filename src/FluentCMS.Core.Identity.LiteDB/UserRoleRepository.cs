using FluentCMS.Core.EventBus.Abstractions;
using FluentCMS.Core.Identity.DocumentDbStores;
using FluentCMS.Core.Identity.Models;
using FluentCMS.Core.Repositories.LiteDB;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Core.Identity.LiteDB;

public class UserRoleRepository<TUserRole>(ILiteDBContext dbContext, ILogger<AuditableEntityRepository<TUserRole>> logger, IEventPublisher eventPublisher, IApplicationExecutionContext executionContext) : AuditableEntityRepository<TUserRole>(dbContext, logger, eventPublisher, executionContext), IUserRoleRepository<TUserRole> where TUserRole : UserRole, new()
{
}
