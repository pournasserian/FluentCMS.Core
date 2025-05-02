using FluentCMS.Core.EventBus.Abstractions;
using FluentCMS.Core.Identity.DocumentDbStores;
using FluentCMS.Core.Identity.Models;
using FluentCMS.Core.Repositories.LiteDB;
using Microsoft.Extensions.Logging;

namespace FluentCMS.Core.Identity.LiteDB;

public class UserRepository<TUser>(ILiteDBContext dbContext, ILogger<AuditableEntityRepository<TUser>> logger, IEventPublisher eventPublisher, IApplicationExecutionContext executionContext) : UserRepository<TUser, Role, UserRole>(dbContext, logger, eventPublisher, executionContext), IUserRepository<TUser>
    where TUser : User
{
}

public class UserRepository<TUser, TRole, TUserRole>(ILiteDBContext dbContext, ILogger<AuditableEntityRepository<TUser>> logger, IEventPublisher eventPublisher, IApplicationExecutionContext executionContext) : UserRepository<TUser, TRole, UserClaim, TUserRole, UserLogin, UserToken>(dbContext, logger, eventPublisher, executionContext), IUserRepository<TUser, TRole, TUserRole>
    where TUser : User
    where TRole : Role
    where TUserRole : UserRole, new()
{
}

public class UserRepository<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken>(ILiteDBContext dbContext, ILogger<AuditableEntityRepository<TUser>> logger, IEventPublisher eventPublisher, IApplicationExecutionContext executionContext) : AuditableEntityRepository<TUser>(dbContext, logger, eventPublisher, executionContext), IUserRepository<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken>
    where TUser : User<TUserClaim, TUserLogin, TUserToken>
    where TRole : Role
    where TUserClaim : UserClaim, new()
    where TUserRole : UserRole, new()
    where TUserLogin : UserLogin, new()
    where TUserToken : UserToken, new()
{
}
