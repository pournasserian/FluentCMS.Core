namespace FluentCMS.Plugins.IdentityManager.Repositories;

public interface IUserRoleRepository : IRepository<UserRole>
{
}

public class UserRoleRepository(
    ApplicationDbContext context,
    ILogger<Repository<UserRole, ApplicationDbContext>> logger) :
    Repository<UserRole, ApplicationDbContext>(context, logger),
    IUserRoleRepository
{
}