using FluentCMS.Core.Identity.DocumentDbStores;
using FluentCMS.Core.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Core.Identity.LiteDB;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiteDbIdentityStore<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>(this IServiceCollection services)
        where TUser : User<TUserClaim, TUserLogin, TUserToken>
        where TRole : Role
        where TUserClaim : UserClaim, new()
        where TUserRole : UserRole, new()
        where TUserLogin : UserLogin, new()
        where TUserToken : UserToken, new()
        where TRoleClaim : RoleClaim, new()
    {
        services.AddScoped<IUserStore<TUser>, DocumentDbUserStore<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>>();
        //services.AddScoped<IRoleStore<TRole, RoleClaim>, DocumentDbRoleStore<TUser, RoleClaim>>();

        services.AddScoped<IUserRepository<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken>, UserRepository<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken>>();
        services.AddScoped<IRoleRepository<TRole, TRoleClaim>, RoleRepository<TRole, TRoleClaim>>();
        services.AddScoped<IUserRoleRepository<TUserRole>, UserRoleRepository<TUserRole>>();
        return services;
    }

    public static IServiceCollection AddLiteDbIdentityStore<TUser, TRole, TUserRole>(this IServiceCollection services)
        where TUser : User
        where TRole : Role
        where TUserRole : UserRole, new()
    {
        services.AddScoped<IUserLoginStore<TUser>, DocumentDbUserStore<TUser, TRole, TUserRole>>();

        services.AddScoped<IUserRepository<TUser, TRole, TUserRole>, UserRepository<TUser, TRole, TUserRole>>();
        services.AddScoped<IRoleRepository<TRole>, RoleRepository<TRole>>();
        services.AddScoped<IUserRoleRepository<TUserRole>, UserRoleRepository<TUserRole>>();
        return services;
    }

    public static IServiceCollection AddLiteDbIdentityStore<TUser>(this IServiceCollection services)
        where TUser : User
    {
        services.AddScoped<IUserLoginStore<TUser>, DocumentDbUserStore<TUser, Role, UserRole>>();

        services.AddScoped<IUserRepository<TUser>, UserRepository<TUser>>();
        services.AddScoped<IRoleRepository<Role>, RoleRepository<Role>>();
        services.AddScoped<IUserRoleRepository<UserRole>, UserRoleRepository<UserRole>>();
        return services;
    }

}
