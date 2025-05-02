namespace FluentCMS.Core.Identity.DocumentDbStores;

public interface IRoleRepository<TRole> : IRoleRepository<TRole, RoleClaim> where TRole : Role
{

}

public interface IRoleRepository<TRole, TRoleClaim> : IAuditableEntityRepository<TRole> where TRole : Role where TRoleClaim : RoleClaim, new()
{

}
