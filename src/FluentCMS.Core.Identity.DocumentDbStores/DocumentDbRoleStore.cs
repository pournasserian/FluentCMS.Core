namespace FluentCMS.Core.Identity.DocumentDbStores;

public class DocumentDbRoleStore<TRole>(IRoleRepository<TRole> roleRepository, ILogger<DocumentDbRoleStore<TRole>> logger, IdentityErrorDescriber? describer = null) : DocumentDbRoleStore<TRole, RoleClaim>(roleRepository, logger, describer) where TRole : Role
{
}

public class DocumentDbRoleStore<TRole, TRoleClaim>(IRoleRepository<TRole, TRoleClaim> roleRepository, ILogger<DocumentDbRoleStore<TRole, TRoleClaim>> logger, IdentityErrorDescriber? describer = null) : IQueryableRoleStore<TRole>, IRoleClaimStore<TRole> where TRole : Role where TRoleClaim : RoleClaim, new()
{
    private bool _disposed;

    public IdentityErrorDescriber ErrorDescriber { get; set; } = describer ?? new IdentityErrorDescriber();

    protected virtual Guid? ConvertIdFromString(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return Guid.Parse(id);
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

    #region IQueryableRoleStore

    public virtual async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);

        try
        {
            await roleRepository.Add(role, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Role {RoleName} created successfully", role.Name);
        }
        catch (RepositoryException)
        {
            logger.LogError("Failed to create role {RoleName}", role.Name);
            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }
        return IdentityResult.Success;
    }

    public virtual async Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        try
        {
            await roleRepository.Update(role, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Role {RoleName} updated successfully", role.Name);
        }
        catch (RepositoryException)
        {
            logger.LogError("Failed to update role {RoleName}", role.Name);
            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }
        return IdentityResult.Success;
    }

    public virtual async Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);

        try
        {
            await roleRepository.Remove(role, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Role {RoleName} deleted successfully", role.Name);
        }
        catch (RepositoryException)
        {
            logger.LogError("Failed to delete role {RoleName}", role.Name);
            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }
        return IdentityResult.Success;
    }

    public virtual Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        return Task.FromResult(ConvertIdToString(role.Id)!);
    }

    public virtual Task<string?> GetRoleNameAsync(TRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        return Task.FromResult(role.Name);
    }

    public virtual Task SetRoleNameAsync(TRole role, string? roleName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public virtual async Task<TRole?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var roleId = ConvertIdFromString(id);
        if (roleId == null)
            return null;
        try
        {
            var role = await roleRepository.GetById(roleId.Value, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("Role {RoleName} found by ID", role.Name);
            return role;
        }
        catch (EntityNotFoundException)
        {
            logger.LogWarning("Role with ID {Id} not found", id);
            return null;
        }
        catch (RepositoryException)
        {
            logger.LogError("Failed to find role by ID {Id}", id);
            throw;
        }
    }

    public virtual async Task<TRole?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        try
        {
            var roles = await roleRepository.Find(r => r.NormalizedName == normalizedName, cancellationToken).ConfigureAwait(false);
            var role = roles.FirstOrDefault();
            if (role == null)
            {
                logger.LogWarning("Role with normalized name {NormalizedName} not found", normalizedName);
                return null;
            }
            logger.LogDebug("Role {RoleName} found by normalized name", role.Name);
            return role;
        }
        catch (Exception)
        {
            logger.LogError("Failed to find role by name {NormalizedName}", normalizedName);
            throw;
        }
    }

    public virtual Task<string?> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        return Task.FromResult(role.NormalizedName);
    }

    public virtual Task SetNormalizedRoleNameAsync(TRole role, string? normalizedName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public virtual IQueryable<TRole> Roles => roleRepository.AsQueryable();

    public void Dispose()
    {
        _disposed = true;
    }

    #endregion    

    #region IRoleClaimStore

    protected virtual TRoleClaim CreateRoleClaim(TRole role, Claim claim)
       => new() { RoleId = role.Id, ClaimType = claim.Type, ClaimValue = claim.Value };

    public virtual async Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(role.Claims);

        var roleClaims = role.Claims.Select(c => new Claim(c.ClaimType!, c.ClaimValue!)).ToList();
        return await Task.FromResult(roleClaims);
    }

    public virtual async Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(role.Claims);
        ArgumentNullException.ThrowIfNull(claim);

        role.Claims.Add(CreateRoleClaim(role, claim));
        await Task.CompletedTask;
    }

    public virtual async Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(role.Claims);
        ArgumentNullException.ThrowIfNull(claim);

        var claims = role.Claims.Where(rc => rc.ClaimValue == claim.Value && rc.ClaimType == claim.Type);
        foreach (var c in claims)
        {
            role.Claims.Remove(c);
        }

        await Task.CompletedTask;
    }

    #endregion

}
