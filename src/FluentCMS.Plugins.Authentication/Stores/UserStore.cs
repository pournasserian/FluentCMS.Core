namespace FluentCMS.Plugins.Authentication.Stores;


public class UserStore(IAuditableEntityRepository<User> userRepository, IAuditableEntityRepository<Role> roleRepository, IAuditableEntityRepository<UserRole> userRoleRepository, ILogger<UserStore<User>> logger, IdentityErrorDescriber describer) : UserStore<User>(userRepository, roleRepository, userRoleRepository, logger, describer)
{
}

public class UserStore<TUser>(IAuditableEntityRepository<TUser> userRepository, IAuditableEntityRepository<Role> roleRepository, IAuditableEntityRepository<UserRole> userRoleRepository, ILogger<UserStore<TUser>> logger, IdentityErrorDescriber describer) : UserStore<TUser, Role, UserClaim, UserRole, UserLogin, UserToken>(userRepository, roleRepository, userRoleRepository, logger, describer)
    where TUser : User<UserClaim, UserLogin, UserToken>
{
}

public class UserStore<TUser, TRole, TUserRole>(IAuditableEntityRepository<TUser> userRepository, IAuditableEntityRepository<TRole> roleRepository, IAuditableEntityRepository<TUserRole> userRoleRepository, ILogger<UserStore<TUser, TRole, TUserRole>> logger, IdentityErrorDescriber describer) : UserStore<TUser, TRole, UserClaim, TUserRole, UserLogin, UserToken>(userRepository, roleRepository, userRoleRepository, logger, describer)
    where TUser : User<UserClaim, UserLogin, UserToken>
    where TRole : Role
    where TUserRole : UserRole, new()
{
}

public class UserStore<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken>(IAuditableEntityRepository<TUser> userRepository, IAuditableEntityRepository<TRole> roleRepository, IAuditableEntityRepository<TUserRole> userRoleRepository, ILogger<UserStore<TUser, TRole, TUserClaim, TUserRole, TUserLogin, TUserToken>> logger, IdentityErrorDescriber describer) : UserStoreBase<TUser, TRole, Guid, TUserClaim, TUserRole, TUserLogin, TUserToken, RoleClaim>(describer),
    IUserLoginStore<TUser>,
    IUserClaimStore<TUser>,
    IUserPasswordStore<TUser>,
    IUserSecurityStampStore<TUser>,
    IUserEmailStore<TUser>,
    IUserLockoutStore<TUser>,
    IUserPhoneNumberStore<TUser>,
    IQueryableUserStore<TUser>,
    IUserTwoFactorStore<TUser>,
    IUserAuthenticationTokenStore<TUser>,
    IUserAuthenticatorKeyStore<TUser>,
    IUserTwoFactorRecoveryCodeStore<TUser>,
    IProtectedUserStore<TUser>,
    IUserRoleStore<TUser>
    where TUser : User<TUserClaim, TUserLogin, TUserToken>
    where TRole : Role
    where TUserClaim : UserClaim, new()
    where TUserRole : UserRole, new()
    where TUserLogin : UserLogin, new()
    where TUserToken : UserToken, new()
{
    #region IUserStore

    public override async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            await userRepository.Add(user, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("User {UserName} created", user.UserName);
            return IdentityResult.Success;
        }
        catch (Exception)
        {
            logger.LogError("User {UserName} creation failed", user.UserName);
            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }
    }

    public override async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            await userRepository.Update(user, cancellationToken).ConfigureAwait(false);
            return IdentityResult.Success;
        }
        catch (Exception)
        {
            logger.LogError("User {UserName} update failed", user.UserName);
            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }
    }

    public override async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            await userRepository.Remove(user, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("User {UserName} deleted", user.UserName);
            return IdentityResult.Success;
        }
        catch (Exception)
        {
            logger.LogError("User {UserName} deletion failed", user.UserName);
            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }
    }

    public override async Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(userId);

        var id = ConvertIdFromString(userId);

        try
        {
            var users = await userRepository.Find(u => u.Id == id, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("User {UserName} found by ID", userId);
            return users.FirstOrDefault();
        }
        catch (Exception)
        {
            logger.LogError("Find failed for UserId {userId} in {FindByIdAsync}", userId, nameof(FindByIdAsync));
            throw;
        }
    }

    public override async Task<TUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(normalizedUserName);

        try
        {
            var users = await userRepository.Find(u => u.NormalizedUserName == normalizedUserName, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("User {UserName} found by name", normalizedUserName);
            return users.FirstOrDefault();
        }
        catch (Exception)
        {
            logger.LogError("Find failed for normalizedUserName {normalizedUserName} in {FindByNameAsync}", normalizedUserName, nameof(FindByNameAsync));
            throw;
        }
    }

    public override async Task<TUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(normalizedEmail);

        try
        {
            var users = await userRepository.Find(u => u.NormalizedEmail == normalizedEmail, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("User {UserName} found by email", normalizedEmail);
            return users.FirstOrDefault();
        }
        catch (Exception)
        {
            logger.LogError("Find failed for normalizedEmail {normalizedEmail} in {FindByEmailAsync}", normalizedEmail, nameof(FindByEmailAsync));
            throw;
        }
    }

    public override IQueryable<TUser> Users
    {
        get { return userRepository.AsQueryable(); }
    }

    protected override async Task<TUser?> FindUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        try
        {
            var users = await userRepository.Find(u => u.Id == userId, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("User {UserName} found by ID", userId);
            return users.FirstOrDefault();
        }
        catch (Exception)
        {
            logger.LogError("Find failed for UserId {userId} in {FindUserAsync}", userId, nameof(FindUserAsync));
            throw;
        }
    }

    #endregion

    #region IUserLoginStore

    public override Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(user.Logins);
        ArgumentNullException.ThrowIfNull(login);
        user.Logins.Add(CreateUserLogin(user, login));
        return Task.FromResult(false);
    }

    public override Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(user.Logins);
        ArgumentNullException.ThrowIfNull(user);
        var entry = user.Logins.Where(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey).FirstOrDefault();
        if (entry != null)
        {
            user.Logins.Remove(entry);
        }
        return Task.FromResult(false);
    }

    public override async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(user.Logins);
        return await Task.FromResult(user.Logins.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName)).ToList()).ConfigureAwait(false);
    }

    public override async Task<TUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var userLogin = await FindUserLoginAsync(loginProvider, providerKey, cancellationToken);
        if (userLogin != null)
        {
            return await FindUserAsync(userLogin.UserId, cancellationToken);
        }
        return null;
    }

    protected override async Task<TUserLogin?> FindUserLoginAsync(Guid userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var user = await FindUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user == null)
        {
            logger.LogDebug("User {UserName} not found", userId);
            return null;
        }

        var userLogins = user.Logins?.Where(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey).ToList() ?? [];

        if (userLogins.Count == 0)
        {
            logger.LogDebug("User {UserName} login {LoginProvider} not found", userId, loginProvider);
            return null;
        }

        if (userLogins.Count > 1)
        {
            logger.LogWarning("User {UserName} has multiple logins for {LoginProvider}", userId, loginProvider);
        }

        return userLogins.FirstOrDefault();
    }

    protected override async Task<TUserLogin?> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        try
        {
            var users = (await userRepository.Find(u => u.Logins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey), cancellationToken).ConfigureAwait(false)).ToList();
            if (users.Count == 0)
            {
                logger.LogDebug("User with login {LoginProvider} not found", loginProvider);
                return null;
            }
            if (users.Count > 1)
            {
                logger.LogWarning("Multiple users found with login {LoginProvider}", loginProvider);
            }

            var user = users.FirstOrDefault();
            return user?.Logins?.FirstOrDefault(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey);
        }
        catch (Exception)
        {
            logger.LogError("Find failed for login {LoginProvider} in {FindUserLoginAsync}", loginProvider, nameof(FindUserLoginAsync));
            throw;
        }
    }

    #endregion

    #region IUserClaimStore

    public override async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(user.Claims);
        ArgumentNullException.ThrowIfNull(claims);
        foreach (var claim in claims)
        {
            var matchedClaims = user.Claims.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type).ToList();
            foreach (var c in matchedClaims)
            {
                user.Claims.Remove(c);
            }
        }
        await Task.CompletedTask;
    }

    public override Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        var userClaims = user.Claims.Where(uc => uc.UserId.Equals(user.Id));
        IList<Claim> claims = userClaims.Select(uc => new Claim(uc.ClaimType!, uc.ClaimValue!)).ToList();
        return Task.FromResult(claims);
    }

    public override Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(user.Claims);
        ArgumentNullException.ThrowIfNull(claims);
        foreach (var claim in claims)
        {
            user.Claims.Add(CreateUserClaim(user, claim));
        }
        return Task.FromResult(false);
    }

    public override Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(user.Claims);
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(newClaim);

        var matchedClaims = user.Claims.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type);
        foreach (var matchedClaim in matchedClaims)
        {
            matchedClaim.ClaimValue = newClaim.Value;
            matchedClaim.ClaimType = newClaim.Type;
        }
        return Task.CompletedTask;
    }

    public override async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(claim);

        try
        {
            var users = await userRepository.Find(u => u.Claims.Any(uc => uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type), cancellationToken).ConfigureAwait(false);
            if (!users.Any())
            {
                logger.LogDebug("No users found for claim {Claim}", claim);
                return [];
            }
            logger.LogDebug("Users found for claim {Claim}", claim);
            return [.. users];
        }
        catch (Exception)
        {
            logger.LogError("Find failed for claim {Claim} in {GetUsersForClaimAsync}", claim, nameof(GetUsersForClaimAsync));
            throw;
        }
    }

    #endregion

    #region IUserAuthenticationTokenStore

    protected override Task<TUserToken?> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(user.Tokens);

        var userTokens = user.Tokens.Where(ut => ut.UserId == user.Id && ut.LoginProvider == loginProvider && ut.Name == name);
        return Task.FromResult(userTokens.FirstOrDefault());
    }

    #endregion

    #region IUserRoleStore

    public override async Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedRoleName);

        var roleEntity = await FindRoleAsync(normalizedRoleName, cancellationToken);
        if (roleEntity == null)
        {
            logger.LogWarning("Role {RoleName} not found", normalizedRoleName);
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Role {0} not found", normalizedRoleName));
        }
        try
        {
            await userRoleRepository.Add(CreateUserRole(user, roleEntity), cancellationToken).ConfigureAwait(false);
            logger.LogInformation("User {UserName} added to role {RoleName}", user.UserName, normalizedRoleName);
        }
        catch (Exception)
        {
            logger.LogError("Failed to add user {UserName} to role {RoleName}", user.UserName, normalizedRoleName);
            throw;
        }
    }

    public override async Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedRoleName);

        try
        {
            var roleEntity = await FindRoleAsync(normalizedRoleName, cancellationToken);
            if (roleEntity != null)
            {
                var userRole = await FindUserRoleAsync(user.Id, roleEntity.Id, cancellationToken);
                if (userRole != null)
                {
                    await userRoleRepository.Remove(userRole, cancellationToken).ConfigureAwait(false);
                }
            }
            logger.LogInformation("User {UserName} removed from role {RoleName}", user.UserName, normalizedRoleName);
        }
        catch (Exception)
        {
            logger.LogError("Failed to remove user {UserName} from role {RoleName}", user.UserName, normalizedRoleName);
            throw;
        }
    }

    public override async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        try
        {
            var roles = await userRoleRepository.Find(ur => ur.UserId == user.Id, cancellationToken).ConfigureAwait(false);
            if (roles == null || !roles.Any())
            {
                logger.LogDebug("No roles found for user {UserName}", user.UserName);
                return [];
            }
            var roleIds = roles.Select(ur => ur.RoleId).ToList();
            var roleEntities = await roleRepository.Find(r => roleIds.Contains(r.Id), cancellationToken).ConfigureAwait(false);
            if (roleEntities == null || !roleEntities.Any())
            {
                logger.LogDebug("No roles found for user {UserName}", user.UserName);
                return [];
            }
            IList<string> roleNames = [.. roleEntities.Select(r => r.Name!)];
            logger.LogDebug("Roles found for user {UserName}", user.UserName);
            return roleNames;
        }
        catch (Exception)
        {
            logger.LogError("Find failed for user {UserName} in {GetRolesAsync}", user.UserName, nameof(GetRolesAsync));
            throw;
        }
    }

    public override async Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedRoleName);

        try
        {
            var role = await FindRoleAsync(normalizedRoleName, cancellationToken).ConfigureAwait(false);
            if (role != null)
            {
                var userRole = await FindUserRoleAsync(user.Id, role.Id, cancellationToken).ConfigureAwait(false);
                return userRole != null;
            }
            return false;
        }
        catch (Exception)
        {
            logger.LogError("Failed to check if user {UserName} is in role {RoleName}", user.UserName, normalizedRoleName);
            throw;
        }
    }

    public override async Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(normalizedRoleName);
        try
        {
            var role = await FindRoleAsync(normalizedRoleName, cancellationToken);

            if (role == null)
            {
                logger.LogWarning("Role {RoleName} not found", normalizedRoleName);
                return [];
            }
            else
            {
                var userRoles = await userRoleRepository.Find(ur => ur.RoleId == role.Id, cancellationToken).ConfigureAwait(false);
                if (userRoles == null || !userRoles.Any())
                {
                    logger.LogDebug("No users found for role {RoleName}", normalizedRoleName);
                    return [];
                }
                var users = await userRepository.Find(u => userRoles.Any(ur => ur.RoleId == role.Id), cancellationToken).ConfigureAwait(false);
                logger.LogDebug("Users found for role {RoleName}", normalizedRoleName);
                return [.. users];
            }
        }
        catch (Exception)
        {
            logger.LogError("Find failed for role {RoleName} in {GetUsersInRoleAsync}", normalizedRoleName, nameof(GetUsersInRoleAsync));
            throw;
        }
    }

    protected override async Task<TRole?> FindRoleAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.Find(r => r.NormalizedName == normalizedRoleName, cancellationToken).ConfigureAwait(false);
        return roles.FirstOrDefault();
    }

    protected override async Task<TUserRole?> FindUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await FindUserAsync(userId, cancellationToken).ConfigureAwait(false);
            if (user == null)
            {
                logger.LogError("User {UserName} not found", userId);
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            var role = await FindRoleAsync(roleId.ToString(), cancellationToken).ConfigureAwait(false);
            if (role == null)
            {
                logger.LogError("Role {RoleName} not found", roleId);
                throw new InvalidOperationException($"Role with ID {roleId} not found");
            }

            var userRoles = (await userRoleRepository.Find(ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken).ConfigureAwait(false)).ToList();

            if (userRoles.Count > 1)
                logger.LogWarning("User {UserName} has multiple roles for {RoleId}", user.UserName, roleId);

            return userRoles.FirstOrDefault();
        }
        catch (Exception)
        {
            logger.LogError("Find failed for user {UserName} and role {RoleId} in {FindUserRoleAsync}", userId, roleId, nameof(FindUserRoleAsync));
            throw;
        }
    }

    #endregion

    #region IUserAuthenticationTokenStore

    protected override async Task AddUserTokenAsync(TUserToken token)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(token);

        try
        {
            var user = await FindUserAsync(token.UserId, CancellationToken.None).ConfigureAwait(false) ??
                throw new InvalidOperationException($"User with ID {token.UserId} not found");

            if (user.Tokens == null)
                throw new InvalidOperationException($"User with ID {token.UserId} has no tokens collection");
            user.Tokens.Add(token);
            logger.LogInformation("Token {Name} added for user {UserName}", token.Name, user.UserName);
        }
        catch (Exception)
        {
            logger.LogError("Failed to add token {Name} for user {UserName}", token.Name, token.UserId);
            throw;
        }
    }

    protected override async Task RemoveUserTokenAsync(TUserToken token)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(token);

        try
        {
            var user = await FindUserAsync(token.UserId, CancellationToken.None).ConfigureAwait(false) ??
                throw new InvalidOperationException($"User with ID {token.UserId} not found");

            if (user.Tokens == null)
                throw new InvalidOperationException($"User with ID {token.UserId} has no tokens collection");

            user.Tokens.Remove(token);

            logger.LogInformation("Token {Name} removed from user {UserName}", token.Name, user.UserName);
        }
        catch (Exception)
        {
            logger.LogError("Failed to add token {Name} for user {UserName}", token.Name, token.UserId);
            throw;
        }
    }

    #endregion
}

