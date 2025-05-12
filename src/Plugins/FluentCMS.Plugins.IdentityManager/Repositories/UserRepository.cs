namespace FluentCMS.Plugins.IdentityManager.Repositories;

public interface IUserRepository : IRepository<User>
{
}

public class UserRepository(
    ApplicationDbContext context,
    ILogger<Repository<User, ApplicationDbContext>> logger) :
    Repository<User, ApplicationDbContext>(context, logger),
    IUserRepository
{
}