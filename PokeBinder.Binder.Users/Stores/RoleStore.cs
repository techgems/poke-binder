using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Binders.DbContext.Entities;

namespace PokeBinder.Binders.Users.Stores;

public class RoleStore :
    IRoleStore<Role>,
    IRoleClaimStore<Role>
{
    private readonly BinderDbContext _db;
    private bool _disposed;

    public RoleStore(BinderDbContext db)
    {
        _db = db;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(GetType().Name);
    }

    public void Dispose() => _disposed = true;

    // ---- IRoleStore ----

    public async Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        _db.Roles.Update(role);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure", Description = "Optimistic concurrency failure, object has been modified." });
        }
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        cancellationToken.ThrowIfCancellationRequested();
        _db.Roles.Remove(role);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure", Description = "Optimistic concurrency failure, object has been modified." });
        }
        return IdentityResult.Success;
    }

    public Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        return Task.FromResult(role.Id.ToString(CultureInfo.InvariantCulture));
    }

    public Task<string?> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        return Task.FromResult(role.Name);
    }

    public Task SetRoleNameAsync(Role role, string? roleName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        return Task.FromResult(role.Name);
    }

    public Task SetNormalizedRoleNameAsync(Role role, string? normalizedName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        role.Name = normalizedName;
        return Task.CompletedTask;
    }

    public Task<Role?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (!int.TryParse(roleId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return Task.FromResult<Role?>(null);
        return _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<Role?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _db.Roles.FirstOrDefaultAsync(r => r.Name == normalizedRoleName, cancellationToken);
    }

    // ---- IRoleClaimStore ----

    public async Task<IList<Claim>> GetClaimsAsync(Role role, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        var claims = await _db.RoleClaims
            .Where(c => c.RoleId == role.Id)
            .Select(c => new Claim(c.ClaimType ?? string.Empty, c.ClaimValue ?? string.Empty))
            .ToListAsync(cancellationToken);
        return claims;
    }

    public Task AddClaimAsync(Role role, Claim claim, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(claim);
        _db.RoleClaims.Add(new RoleClaim { RoleId = role.Id, ClaimType = claim.Type, ClaimValue = claim.Value });
        return Task.CompletedTask;
    }

    public async Task RemoveClaimAsync(Role role, Claim claim, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(claim);
        var matches = await _db.RoleClaims
            .Where(c => c.RoleId == role.Id && c.ClaimType == claim.Type && c.ClaimValue == claim.Value)
            .ToListAsync(cancellationToken);
        _db.RoleClaims.RemoveRange(matches);
    }
}
