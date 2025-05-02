namespace FluentCMS.Core.Identity.DocumentDbStores;

public interface IUserRoleRepository<TUserRole> : IAuditableEntityRepository<TUserRole> where TUserRole : UserRole, new()
{
}
