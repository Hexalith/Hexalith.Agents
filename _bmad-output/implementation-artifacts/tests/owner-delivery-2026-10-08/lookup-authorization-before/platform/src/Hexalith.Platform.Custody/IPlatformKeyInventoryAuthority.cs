namespace Hexalith.Platform.Custody;

/// <summary>Independent exact key provision/revocation authority, not custodian or recorder self-approval.</summary>
public interface IPlatformKeyInventoryAuthority
{
    /// <summary>Authenticates exact tenant/purpose/version/provider/authority and current complete change evidence.</summary>
    Task<bool> AuthorizeAsync(PlatformKeyInventoryChange change, CancellationToken cancellationToken = default);
}
