namespace FluentCMS.Plugins.Authentication.Stores;

public class UserStore<TUser, TRole, TContext, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>(IAuditableEntityRepository<TUser> userRepository, IAuditableEntityRepository<TRole> roleRepository, IAuditableEntityRepository<TUserRole> userRoleRepository, IAuditableEntityRepository<TUserLogin> userLoginRepository, IAuditableEntityRepository<TUserClaim> userClaimsRepository, IAuditableEntityRepository<TUserToken> userTokenRepository) :
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

    #region IUserStore

    public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default)
    {
        await userRepository.Add(user, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
    {
        await userRepository.Update(user, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
    {
        await userRepository.Remove(user, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var id = ConvertIdFromString(userId);
        if (id is null)
            return default;

        var users = await userRepository.Find(u => u.Id == id.Value, cancellationToken);
        return users.FirstOrDefault();
    }

    public async Task<TUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
    {
        var users = await userRepository.Find(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
        return users.FirstOrDefault();
    }

    public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(ConvertIdToString(user.Id)!);
    }

    public Task<string?> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task SetUserNameAsync(TUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task SetNormalizedUserNameAsync(TUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public async Task<TUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var users = await userRepository.Find(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        return users.FirstOrDefault();
    }

    public IQueryable<TUser> Users
    {
        get { return userRepository.AsQueryable(); }
    }

    protected async Task<TUser?> FindUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var users = await userRepository.Find(u => u.Id == userId, cancellationToken);
        return users.FirstOrDefault();
    }

    public void Dispose()
    {
        _disposed = true;
    }

    #endregion

    #region IUserLoginStore

    public async Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default)
    {
        await userLoginRepository.Add(new TUserLogin
        {
            UserId = user.Id,
            LoginProvider = login.LoginProvider,
            ProviderKey = login.ProviderKey,
            ProviderDisplayName = login.ProviderDisplayName
        }, cancellationToken);
    }

    public async Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
    {
        var entry = await FindUserLoginAsync(user.Id, loginProvider, providerKey, cancellationToken);
        if (entry != null)
        {
            await userLoginRepository.Remove(entry, cancellationToken);
        }
    }

    public async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default)
    {
        var userId = user.Id;
        var userLogins = await userLoginRepository.Find(l => l.UserId.Equals(userId), cancellationToken);
        return userLogins.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName)).ToList();
    }

    public async Task<TUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
    {
        var userLogin = await FindUserLoginAsync(loginProvider, providerKey, cancellationToken);
        if (userLogin != null)
        {
            return await FindUserAsync(userLogin.UserId, cancellationToken);
        }
        return null;
    }

    protected async Task<TUserLogin?> FindUserLoginAsync(Guid userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        var userLogins = await userLoginRepository.Find(userLogin => userLogin.UserId == userId && userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey, cancellationToken);
        return userLogins.FirstOrDefault();
    }

    protected async Task<TUserLogin?> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        var userLogins = await userLoginRepository.Find(userLogin => userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey, cancellationToken);
        return userLogins.FirstOrDefault();
    }

    #endregion

    #region IUserClaimStore

    public async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
    {
        var userId = user.Id;
        foreach (var claim in claims)
        {
            var matchedClaims = await userClaimsRepository.Find(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type, cancellationToken);
            foreach (var c in matchedClaims)
            {
                await userClaimsRepository.Remove(c, cancellationToken);
            }
        }
    }

    public async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default)
    {
        var userClaims = await userClaimsRepository.Find(uc => uc.UserId.Equals(user.Id), cancellationToken);
        return [.. userClaims.Select(uc => new Claim(uc.ClaimType!, uc.ClaimValue!))];
    }

    public async Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
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
            await userClaimsRepository.Add(userClaim, cancellationToken);
        }
    }

    public async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
    {
        var matchedClaims = await userClaimsRepository.Find(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type, cancellationToken);
        foreach (var matchedClaim in matchedClaims)
        {
            matchedClaim.ClaimValue = newClaim.Value;
            matchedClaim.ClaimType = newClaim.Type;
        }
    }

    public async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        var userClaims = await userClaimsRepository.Find(uc => uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type, cancellationToken);
        var userIds = userClaims.Select(uc => uc.UserId);
        var users = await userRepository.Find(u => userIds.Contains(u.Id), cancellationToken);
        return [.. users];
    }

    #endregion

    #region IUserPasswordStore

    public Task SetPasswordHashAsync(TUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
    }

    #endregion

    #region IUserSecurityStampStore

    public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.SecurityStamp);
    }

    #endregion

    #region IUserEmailStore

    public Task SetEmailAsync(TUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.EmailConfirmed);
    }

    public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(TUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserLockoutStore

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnd);
    }

    public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnabled);
    }

    public Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserPhoneNumberStore

    public Task SetPhoneNumberAsync(TUser user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    public Task<string?> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumber);
    }

    public Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.PhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserTwoFactorStore

    public Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    public Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken)
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
            await userTokenRepository.Add(newToken, cancellationToken);
        }
        else
        {
            token.Value = value;
            await userTokenRepository.Update(token, cancellationToken);
        }
    }

    public virtual async Task RemoveTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        var entry = await FindTokenAsync(user, loginProvider, name, cancellationToken).ConfigureAwait(false);
        if (entry != null)
        {
            await userTokenRepository.Remove(entry, cancellationToken);
        }
    }

    public virtual async Task<string?> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        var entry = await FindTokenAsync(user, loginProvider, name, cancellationToken).ConfigureAwait(false);
        return entry?.Value;
    }

    protected async Task<TUserToken?> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        var userTokens = await userTokenRepository.Find(ut => ut.UserId == user.Id && ut.LoginProvider == loginProvider && ut.Name == name, cancellationToken);
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

    public virtual Task ReplaceCodesAsync(TUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        var mergedCodes = string.Join(";", recoveryCodes);
        return SetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, mergedCodes, cancellationToken);
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

    public async Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        var roleEntity = await FindRoleAsync(normalizedRoleName, cancellationToken) ??
            throw new InvalidOperationException($"Role {normalizedRoleName} not found");

        var userRole = new TUserRole
        {
            UserId = user.Id,
            RoleId = roleEntity.Id
        };

        await userRoleRepository.Add(userRole, cancellationToken);
    }

    public async Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        var roleEntity = await FindRoleAsync(normalizedRoleName, cancellationToken);
        if (roleEntity != null)
        {
            var userRole = await FindUserRoleAsync(user.Id, roleEntity.Id, cancellationToken);
            if (userRole != null)
            {
                await userRoleRepository.Remove(userRole, cancellationToken);
            }
        }
    }

    public async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default)
    {
        var userRoles = await userRoleRepository.Find(ur => ur.UserId == user.Id, cancellationToken);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        var roles = await roleRepository.Find(r => roleIds.Contains(r.Id), cancellationToken);
        return [.. roles.Select(r => r.Name!)];
    }

    public async Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        var role = await FindRoleAsync(normalizedRoleName, cancellationToken);
        if (role != null)
        {
            var userRole = await FindUserRoleAsync(user.Id, role.Id, cancellationToken);
            return userRole != null;
        }
        return false;
    }

    public async Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        var role = await FindRoleAsync(normalizedRoleName, cancellationToken);
        if (role == null)
            return [];

        var userRoles = await userRoleRepository.Find(ur => ur.RoleId == role.Id, cancellationToken);
        var userIds = userRoles.Select(ur => ur.UserId);
        var users = await userRepository.Find(u => userIds.Contains(u.Id), cancellationToken);
        return [.. users];
    }

    protected async Task<TRole?> FindRoleAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.Find(r => r.NormalizedName == normalizedRoleName, cancellationToken);
        return roles.FirstOrDefault();
    }

    protected async Task<TUserRole?> FindUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        var userRoles = await userRoleRepository.Find(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        return userRoles.FirstOrDefault();
    }

    #endregion

    protected virtual Guid? ConvertIdFromString(string? id)
    {
        if (id == null)
        {
            return null;
        }
        return (Guid?)TypeDescriptor.GetConverter(typeof(Guid)).ConvertFromInvariantString(id);
    }

    public virtual string? ConvertIdToString(Guid id)
    {
        if (id == Guid.Empty)
            return null;

        return id.ToString();
    }
}

