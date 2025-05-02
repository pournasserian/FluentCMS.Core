namespace FluentCMS.Core.Identity.DocumentDbStores;

public interface IUserRepository<TUser> : IUserRepository<TUser, Role, UserRole>
    where TUser : User
{
}

public interface IUserRepository<TUser, TRole, TUserRole> : IUserRepository<TUser, TRole, UserClaim, TUserRole, UserLogin, UserToken>
    where TUser : User
    where TRole : Role
    where TUserRole : UserRole, new()
{
}

public interface IUserRepository<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken> : IAuditableEntityRepository<TUser> 
    where TUser : User<TUserClaim, TUserLogin, TUserToken>
    where TRole : Role
    where TUserClaim : UserClaim, new()
    where TUserRole : UserRole, new()
    where TUserLogin : UserLogin, new()
    where TUserToken : UserToken, new()
{
}
