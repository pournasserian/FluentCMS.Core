namespace FluentCMS.Plugins.Authentication.Stores;

public class UserStore<TUser, TRole, TContext, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>(IAuditableEntityRepository<TUser> userRepository, IAuditableEntityRepository<TRole> roleRepository, IAuditableEntityRepository<TUserRole> userRoleRepository, IAuditableEntityRepository<TUserLogin> userLoginRepository, IAuditableEntityRepository<TUserClaim> userClaimsRepository, IAuditableEntityRepository<TUserToken> userTokenRepository, ILogger<UserStore<TUser, TRole, TContext, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>> logger, IdentityErrorDescriber? describer = null) :
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
    where TUser : User
    where TRole : Role
    where TUserClaim : UserClaim, new()
    where TUserRole : UserRole, new()
    where TUserLogin : UserLogin, new()
    where TUserToken : UserToken, new()
    where TRoleClaim : RoleClaim, new()
{
    private bool _disposed;
    private const string InternalLoginProvider = "[FluentCMSIdentity]";
    private const string AuthenticatorKeyTokenName = "AuthenticatorKey";
    private const string RecoveryCodeTokenName = "RecoveryCodes";
    public IdentityErrorDescriber ErrorDescriber { get; set; } = describer ?? new IdentityErrorDescriber();


    protected virtual Guid? ConvertIdFromString(string? id)
    {
        if (id == null)
        {
            return null;
        }
        return (Guid?)TypeDescriptor.GetConverter(typeof(Guid)).ConvertFromInvariantString(id);
    }

    protected virtual string? ConvertIdToString(Guid id)
    {
        if (id == Guid.Empty)
            return null;

        return id.ToString();
    }

    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    #region IUserStore

    public virtual async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default)
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

    public virtual async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
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

    public virtual async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
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

    public virtual async Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(userId);

        var id = ConvertIdFromString(userId);
        if (id is null)
            return default;

        try
        {
            var users = await userRepository.Find(u => u.Id == id.Value, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("User {UserName} found by ID", userId);
            return users.FirstOrDefault();
        }
        catch (Exception)
        {
            logger.LogError("Exception finding {userId} in {FindByIdAsync}", userId, nameof(FindByIdAsync));
            throw;
        }
    }

    public virtual async Task<TUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
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
            logger.LogError("Exception finding {normalizedUserName} in {FindByNameAsync}", normalizedUserName, nameof(FindByNameAsync));
            throw;
        }
    }

    public virtual async Task<TUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
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
            logger.LogError("Exception finding {normalizedEmail} in {FindByEmailAsync}", normalizedEmail, nameof(FindByEmailAsync));
            throw;
        }
    }

    public virtual Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(ConvertIdToString(user.Id)!);
    }

    public virtual Task<string?> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public virtual Task SetUserNameAsync(TUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public virtual Task<string?> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedUserName);
    }

    public virtual Task SetNormalizedUserNameAsync(TUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public virtual IQueryable<TUser> Users
    {
        get { return userRepository.AsQueryable(); }
    }

    protected virtual async Task<TUser?> FindUserAsync(Guid userId, CancellationToken cancellationToken)
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
            logger.LogError("Exception finding {userId} in {FindUserAsync}", userId, nameof(FindUserAsync));
            throw;
        }
    }

    public virtual void Dispose()
    {
        _disposed = true;
    }

    #endregion

    #region IUserLoginStore

    protected virtual TUserLogin CreateUserLogin(TUser user, UserLoginInfo login)
    {
        return new TUserLogin
        {
            UserId = user.Id,
            LoginProvider = login.LoginProvider,
            ProviderKey = login.ProviderKey,
            ProviderDisplayName = login.ProviderDisplayName
        };
    }

    public virtual async Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(login);

        try
        {
            await userLoginRepository.Add(CreateUserLogin(user, login), cancellationToken).ConfigureAwait(false);
            logger.LogInformation("User {UserName} added login {LoginProvider}", user.UserName, login.LoginProvider);
        }
        catch (Exception)
        {
            logger.LogError("User {UserName} failed to add login {LoginProvider}", user.UserName, login.LoginProvider);
            throw;
        }
    }

    public virtual async Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            var entry = await FindUserLoginAsync(user.Id, loginProvider, providerKey, cancellationToken).ConfigureAwait(false);
            if (entry != null)
            {
                await userLoginRepository.Remove(entry, cancellationToken).ConfigureAwait(false);
            }
            logger.LogInformation("User {UserName} removed login {LoginProvider}", user.UserName, loginProvider);
        }
        catch (Exception)
        {
            logger.LogError("User {UserName} failed to remove login {LoginProvider}", user.UserName, loginProvider);
            throw;
        }
    }

    public virtual async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        try
        {

            var userId = user.Id;
            var userLogins = await userLoginRepository.Find(l => l.UserId.Equals(userId), cancellationToken).ConfigureAwait(false);
            logger.LogDebug("User {UserName} logins found", user.UserName);
            return [.. userLogins.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName))];
        }
        catch (Exception)
        {
            logger.LogError("Exception finding {userId} in {GetLoginsAsync}", user.Id, nameof(GetLoginsAsync));
            throw;
        }
    }

    public virtual async Task<TUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var userLogin = await FindUserLoginAsync(loginProvider, providerKey, cancellationToken);
        if (userLogin != null)
        {
            return await FindUserAsync(userLogin.UserId, cancellationToken).ConfigureAwait(false);
        }
        return null;
    }

    protected virtual async Task<TUserLogin?> FindUserLoginAsync(Guid userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        try
        {
            var userLogins = await userLoginRepository.Find(userLogin => userLogin.UserId == userId && userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("User {UserName} login {LoginProvider} found", userId, loginProvider);
            return userLogins.FirstOrDefault();
        }
        catch (Exception)
        {
            logger.LogError("Exception finding {userId} in {FindUserLoginAsync}", userId, nameof(FindUserLoginAsync));
            throw;
        }
    }

    protected virtual async Task<TUserLogin?> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        try
        {
            var userLogins = await userLoginRepository.Find(userLogin => userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey, cancellationToken);
            logger.LogDebug("User login {LoginProvider} found", loginProvider);
            return userLogins.FirstOrDefault();
        }
        catch (Exception)
        {
            logger.LogError("Exception finding {loginProvider} in {FindUserLoginAsync}", loginProvider, nameof(FindUserLoginAsync));
            throw;
        }
    }

    #endregion

    #region IUserClaimStore

    public virtual async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
    {
        var userId = user.Id;
        foreach (var claim in claims)
        {
            var matchedClaims = await userClaimsRepository.Find(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type, cancellationToken).ConfigureAwait(false);
            foreach (var c in matchedClaims)
            {
                await userClaimsRepository.Remove(c, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public virtual async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default)
    {
        var userClaims = await userClaimsRepository.Find(uc => uc.UserId.Equals(user.Id), cancellationToken).ConfigureAwait(false);
        return [.. userClaims.Select(uc => new Claim(uc.ClaimType!, uc.ClaimValue!))];
    }

    public virtual async Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
    {
        foreach (var claim in claims)
        {
            var userClaim = new TUserClaim
            {
                UserId = user.Id,
                ClaimType = claim.Type,
                ClaimValue = claim.Value
            };
            userClaim.InitializeFromClaim(claim);
            await userClaimsRepository.Add(userClaim, cancellationToken).ConfigureAwait(false);
        }
    }

    public virtual async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
    {
        var matchedClaims = await userClaimsRepository.Find(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type, cancellationToken).ConfigureAwait(false);
        foreach (var matchedClaim in matchedClaims)
        {
            matchedClaim.ClaimValue = newClaim.Value;
            matchedClaim.ClaimType = newClaim.Type;
        }
    }

    public virtual async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        var userClaims = await userClaimsRepository.Find(uc => uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type, cancellationToken).ConfigureAwait(false);
        var userIds = userClaims.Select(uc => uc.UserId);
        var users = await userRepository.Find(u => userIds.Contains(u.Id), cancellationToken);
        return [.. users];
    }

    #endregion

    #region IUserPasswordStore

    public virtual Task SetPasswordHashAsync(TUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public virtual Task<string?> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash);
    }

    public virtual Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
    }

    #endregion

    #region IUserSecurityStampStore

    public virtual Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public virtual Task<string?> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.SecurityStamp);
    }

    #endregion

    #region IUserEmailStore

    public virtual Task SetEmailAsync(TUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public virtual Task<string?> GetEmailAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email);
    }

    public virtual Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.EmailConfirmed);
    }

    public virtual Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public virtual Task<string?> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedEmail);
    }

    public virtual Task SetNormalizedEmailAsync(TUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserLockoutStore

    public virtual Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnd);
    }

    public virtual Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public virtual Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public virtual Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public virtual Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.AccessFailedCount);
    }

    public virtual Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnabled);
    }

    public virtual Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserPhoneNumberStore

    public virtual Task SetPhoneNumberAsync(TUser user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    public virtual Task<string?> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumber);
    }

    public virtual Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    public virtual Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.PhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserTwoFactorStore

    public virtual Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    public virtual Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.TwoFactorEnabled);
    }

    #endregion

    #region IUserAuthenticationTokenStore

    public virtual async Task SetTokenAsync(TUser user, string loginProvider, string name, string? value, CancellationToken cancellationToken)
    {
        var token = await FindTokenAsync(user, loginProvider, name, cancellationToken).ConfigureAwait(false);
        if (token == null)
        {
            var newToken = new TUserToken
            {
                UserId = user.Id,
                LoginProvider = loginProvider,
                Name = name,
                Value = value
            };
            await userTokenRepository.Add(newToken, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            token.Value = value;
            await userTokenRepository.Update(token, cancellationToken).ConfigureAwait(false);
        }
    }

    public virtual async Task RemoveTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        var entry = await FindTokenAsync(user, loginProvider, name, cancellationToken).ConfigureAwait(false);
        if (entry != null)
        {
            await userTokenRepository.Remove(entry, cancellationToken).ConfigureAwait(false);
        }
    }

    public virtual async Task<string?> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        var entry = await FindTokenAsync(user, loginProvider, name, cancellationToken).ConfigureAwait(false);
        return entry?.Value;
    }

    protected async Task<TUserToken?> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        var userTokens = await userTokenRepository.Find(ut => ut.UserId == user.Id && ut.LoginProvider == loginProvider && ut.Name == name, cancellationToken).ConfigureAwait(false);
        return userTokens.FirstOrDefault();
    }

    #endregion

    #region IUserAuthenticatorKeyStore

    public Task SetAuthenticatorKeyAsync(TUser user, string key, CancellationToken cancellationToken)
    {
        user.AuthenticatorKey = key;
        return Task.CompletedTask;
    }

    public Task<string?> GetAuthenticatorKeyAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.AuthenticatorKey);
    }

    #endregion

    #region IUserTwoFactorRecoveryCodeStore

    public virtual async Task ReplaceCodesAsync(TUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        var mergedCodes = string.Join(";", recoveryCodes);
        await SetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, mergedCodes, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<bool> RedeemCodeAsync(TUser user, string code, CancellationToken cancellationToken)
    {
        var mergedCodes = await GetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken).ConfigureAwait(false) ?? "";
        var splitCodes = mergedCodes.Split(';');
        if (splitCodes.Contains(code))
        {
            var updatedCodes = new List<string>(splitCodes.Where(s => s != code));
            await ReplaceCodesAsync(user, updatedCodes, cancellationToken).ConfigureAwait(false);
            return true;
        }
        return false;
    }

    public virtual async Task<int> CountCodesAsync(TUser user, CancellationToken cancellationToken)
    {
        var mergedCodes = await GetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken).ConfigureAwait(false) ?? "";
        if (mergedCodes.Length > 0)
        {
            return mergedCodes.AsSpan().Count(';') + 1;
        }
        return 0;
    }

    #endregion

    #region IUserRoleStore

    public virtual async Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        var roleEntity = await FindRoleAsync(normalizedRoleName, cancellationToken).ConfigureAwait(false);

        var userRole = new TUserRole
        {
            UserId = user.Id,
            RoleId = roleEntity.Id
        };

        await userRoleRepository.Add(userRole, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        var roleEntity = await FindRoleAsync(normalizedRoleName, cancellationToken).ConfigureAwait(false);
        if (roleEntity != null)
        {
            var userRole = await FindUserRoleAsync(user.Id, roleEntity.Id, cancellationToken).ConfigureAwait(false);
            if (userRole != null)
            {
                await userRoleRepository.Remove(userRole, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public virtual async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default)
    {
        var userRoles = await userRoleRepository.Find(ur => ur.UserId == user.Id, cancellationToken).ConfigureAwait(false);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        var roles = await roleRepository.Find(r => roleIds.Contains(r.Id), cancellationToken).ConfigureAwait(false);
        return [.. roles.Select(r => r.Name!)];
    }

    public virtual async Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        var role = await FindRoleAsync(normalizedRoleName, cancellationToken).ConfigureAwait(false);
        if (role != null)
        {
            var userRole = await FindUserRoleAsync(user.Id, role.Id, cancellationToken).ConfigureAwait(false);
            return userRole != null;
        }
        return false;
    }

    public virtual async Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        var role = await FindRoleAsync(normalizedRoleName, cancellationToken).ConfigureAwait(false);
        if (role == null)
            return [];

        var userRoles = await userRoleRepository.Find(ur => ur.RoleId == role.Id, cancellationToken).ConfigureAwait(false);
        var userIds = userRoles.Select(ur => ur.UserId);
        var users = await userRepository.Find(u => userIds.Contains(u.Id), cancellationToken).ConfigureAwait(false);
        return [.. users];
    }

    protected virtual async Task<TRole?> FindRoleAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.Find(r => r.NormalizedName == normalizedRoleName, cancellationToken).ConfigureAwait(false);
        return roles.FirstOrDefault();
    }

    protected virtual async Task<TUserRole?> FindUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        var userRoles = await userRoleRepository.Find(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken).ConfigureAwait(false);
        return userRoles.FirstOrDefault();
    }

    #endregion

}

