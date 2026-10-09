namespace Hexalith.EventStore.Contracts.Streams;

/// <summary>Bounded independently authenticated original reconciliation work. Partial sources never certify a publication checkpoint.</summary>
/// <param name="Scope">Exact installed namespace.</param><param name="Version">Conditional proof generation.</param><param name="SourceAuthorityRevision">Original complete-cut authority.</param>
/// <param name="Sources">Exact entire complete cut, in canonical source order.</param><param name="VerifiedSources">Consecutive independently verified source prefixes.</param>
/// <param name="Publications">Exact independently verified closed projections of those prefixes.</param><param name="ReceiptId">Independent immutable original proof; its text alone is insufficient.</param>
public sealed record SourcePublicationReconciliationProgress(SourcePublicationScope Scope, long Version, string SourceAuthorityRevision,
    IReadOnlyList<SourcePublicationHead> Sources, int VerifiedSources, IReadOnlyList<SourcePublicationDescriptor> Publications, string ReceiptId)
{
    /// <summary>Checks structural exact-cut identity only; independent fresh proof verification remains mandatory.</summary>
    public bool MatchesCut(SourcePublicationCut cut)
    {
        ArgumentNullException.ThrowIfNull(cut);
        return Scope == cut.Scope && SourceAuthorityRevision == cut.AuthorityRevision && Sources.SequenceEqual(cut.Sources);
    }

    /// <summary>Captures bounded owned proof carriers without claiming their authenticity or availability.</summary>
    public static SourcePublicationReconciliationProgress Capture(SourcePublicationReconciliationProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        if (progress.Scope is null || progress.Version <= 0 || string.IsNullOrWhiteSpace(progress.SourceAuthorityRevision)
            || progress.SourceAuthorityRevision.Length > 256 || string.IsNullOrWhiteSpace(progress.ReceiptId) || progress.ReceiptId.Length > 256
            || progress.Sources is null || progress.Sources.Count is < 0 or > 1000 || progress.VerifiedSources <= 0
            || progress.VerifiedSources > progress.Sources.Count || progress.Publications is null || progress.Publications.Count is < 0 or > 10000)
        { throw new InvalidOperationException("Malformed source reconciliation proof."); }
        var heads = new List<SourcePublicationHead>();
        foreach (var head in progress.Sources)
        {
            if (heads.Count >= 1000 || head is null || head.Identity is null || head.Identity.TenantId != progress.Scope.Tenant
                || head.Identity.Domain != progress.Scope.Domain || head.Head is < 0 or > 10000
                || heads.Count > 0 && StringComparer.Ordinal.Compare(heads[^1].Identity.ActorId, head.Identity.ActorId) >= 0)
            { throw new InvalidOperationException("Malformed reconciliation source vector."); }
            heads.Add(head);
        }
        if (progress.VerifiedSources > heads.Count) { throw new InvalidOperationException("Malformed reconciliation prefix."); }
        var publications = new List<SourcePublicationDescriptor>();
        foreach (var item in progress.Publications)
        {
            if (publications.Count >= 10000 || item is null || item.Identity is null || item.SourceRevision is < 1 or > 10000
                || string.IsNullOrWhiteSpace(item.PublicationId) || item.PublicationId.Length > 256 || string.IsNullOrWhiteSpace(item.SourceMessageId) || item.SourceMessageId.Length > 256
                || item.StableFieldsDigest is not { Length: 64 } digest || !digest.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F')
                || !heads.Take(progress.VerifiedSources).Any(h => h.Identity == item.Identity && h.Head >= item.SourceRevision)
                || publications.Any(p => p.PublicationId == item.PublicationId || p.Identity == item.Identity && p.SourceRevision == item.SourceRevision))
            { throw new InvalidOperationException("Malformed reconciliation publication proof."); }
            publications.Add(item);
        }
        return progress with { Sources = heads.AsReadOnly(), Publications = publications.AsReadOnly() };
    }
}
