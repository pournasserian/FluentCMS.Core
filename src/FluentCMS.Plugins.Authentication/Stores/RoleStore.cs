namespace FluentCMS.Plugins.Authentication.Stores;

public class RoleStore<TRole, TRoleClaim>(IAuditableEntityRepository<TRole> roleRepository, IAuditableEntityRepository<TRoleClaim> roleClaimsRepository, IdentityErrorDescriber? describer = null) : IQueryableRoleStore<TRole>, IRoleClaimStore<TRole> where TRole : Role where TRoleClaim : RoleClaim, new()
{
    private bool _disposed;

    public IdentityErrorDescriber ErrorDescriber { get; set; } = describer ?? new IdentityErrorDescriber();

    public virtual Guid? ConvertIdFromString(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return Guid.Parse(id);
    }

    public virtual string? ConvertIdToString(Guid id)
    {
        if (id == Guid.Empty)
            return null;
        return id.ToString();
    }
    #region IQueryableRoleStore

    public virtual async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        await roleRepository.Add(role, cancellationToken).ConfigureAwait(false);
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
        }
        catch (RepositoryException)
        {
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
        }
        catch (RepositoryException)
        {
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

        return await roleRepository.GetById(roleId.Value, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<TRole?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        var roles = await roleRepository.Find(r => r.NormalizedName == normalizedName, cancellationToken).ConfigureAwait(false);
        var role = roles.FirstOrDefault();
        return role;
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

    public void Dispose() => _disposed = true;

    #endregion

    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    #region IRoleClaimStore

    protected virtual TRoleClaim CreateRoleClaim(TRole role, Claim claim)
       => new() { RoleId = role.Id, ClaimType = claim.Type, ClaimValue = claim.Value };

    public virtual async Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        var roleClaims = await roleClaimsRepository.Find(rc => rc.RoleId.Equals(role.Id), cancellationToken).ConfigureAwait(false);
        return [.. roleClaims.Select(c => new Claim(c.ClaimType!, c.ClaimValue!))];
    }

    public virtual async Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(claim);

        var roleClaim = CreateRoleClaim(role, claim);
        await roleClaimsRepository.Add(roleClaim, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(claim);

        var claims = await roleClaimsRepository.Find(rc => rc.RoleId.Equals(role.Id) && rc.ClaimValue == claim.Value && rc.ClaimType == claim.Type, cancellationToken).ConfigureAwait(false);
        foreach (var c in claims)
            await roleClaimsRepository.Remove(c, cancellationToken).ConfigureAwait(false);
    }

    #endregion

}
