namespace FluentCMS.Plugins.IdentityManager.Repositories;

public interface IUserRepository : IRepository<User>
{
}

public class UserRepository(ApplicationDbContext context) :
    Repository<User, ApplicationDbContext>(context),
    IUserRepository
{
}