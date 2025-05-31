namespace FluentCMS.Plugins.IdentityManager.Repositories;

public interface IUserRoleRepository : IRepository<UserRole>
{
}

public class UserRoleRepository(ApplicationDbContext context) :
    Repository<UserRole, ApplicationDbContext>(context),
    IUserRoleRepository
{
}