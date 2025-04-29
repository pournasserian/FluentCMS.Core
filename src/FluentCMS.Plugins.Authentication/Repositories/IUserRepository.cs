using FluentCMS.Core.Repositories.Abstractions;
using FluentCMS.Plugins.Authentication.Models;
using System.Security.Claims;

namespace FluentCMS.Plugins.Authentication.Repositories;

public interface IUserRepository : IEntityRepository<User>
{
    IQueryable<User> AsQueryable();
    Task<IList<User>> GetUsersForClaim(Claim claim, CancellationToken cancellationToken = default);
    Task<User?> FindByEmail(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<User?> FindByLogin(string loginProvider, string providerKey, CancellationToken cancellationToken = default);
    Task<User?> FindByName(string normalizedUserName, CancellationToken cancellationToken = default);
}
