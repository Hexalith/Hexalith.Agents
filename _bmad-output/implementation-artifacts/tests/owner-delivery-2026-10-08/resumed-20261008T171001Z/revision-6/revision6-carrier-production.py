from pathlib import Path
p=Path('/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody/PrivateOwnerOperationAuthenticator.cs');s=p.read_text().replace('using System.Globalization;','using System.Globalization;\nusing System.Text;')
s=s.replace('''            if (grants is null || caller.Identities.Count(i => i.IsAuthenticated) != 1
                || await AwaitAsync(() => Task.FromResult(profiles.GetCurrent()), start, token).ConfigureAwait(false) is not { } profile || !profile.IsValid(clock.GetUtcNow())) { return null; }
            ClaimsIdentity machine = caller.Identities.Single(i => i.IsAuthenticated);
            string? issuer = One(machine, "iss"), subject = One(machine, "sub"), client = One(machine, "azp"), audience = One(machine, "aud");
            if (issuer is null || subject is null || client is null || audience is null) { return null; }''','''            if (grants is null || !ValidScope(expected) || input is not null && !ValidCredentialIdentities(input)) { return null; }
            var machine = await AwaitAsync(() => Task.FromResult(CaptureMachine(caller)), start, token).ConfigureAwait(false);
            if (machine is null || await AwaitAsync(() => Task.FromResult(profiles.GetCurrent()), start, token).ConfigureAwait(false) is not { } profile
                || !ValidText(profile.Version) || !ValidText(profile.Issuer) || !ValidText(profile.Audience) || !profile.IsValid(clock.GetUtcNow())) { return null; }
            string issuer = machine.Issuer, subject = machine.Subject, client = machine.Client, audience = machine.Audience;''')
s=s.replace('!string.IsNullOrWhiteSpace(grant.AuthorityReference)', 'ValidText(grant.AuthorityReference)')
s=s.replace('''    { var values = caller.FindAll(name).ToArray(); return values.Length == 1 && !string.IsNullOrWhiteSpace(values[0].Value) ? values[0].Value : null; }''','''    {
        string? value = null;
        foreach (Claim claim in caller.FindAll(name))
        {
            if (value is not null || !ValidText(claim.Value)) { return null; }
            value = claim.Value;
        }
        return value;
    }
    private sealed record Machine(string Issuer, string Subject, string Client, string Audience);
    private static Machine? CaptureMachine(ClaimsPrincipal caller)
    {
        ClaimsIdentity? identity = null;
        foreach (ClaimsIdentity candidate in caller.Identities)
        { if (candidate.IsAuthenticated) { if (identity is not null) { return null; } identity = candidate; } }
        if (identity is null) { return null; }
        string? issuer = One(identity, "iss"), subject = One(identity, "sub"), client = One(identity, "azp"), audience = One(identity, "aud");
        return issuer is not null && subject is not null && client is not null && audience is not null ? new(issuer, subject, client, audience) : null;
    }
    private static bool ValidText(string? value)
    {
        try { return !string.IsNullOrWhiteSpace(value) && value.Length <= 2048 && new UTF8Encoding(false, true).GetByteCount(value) <= 2048; }
        catch (EncoderFallbackException) { return false; }
    }
    private static bool ValidScope(PrivateOwnerOperationScope scope)
        => scope.PayloadFingerprint is { Length: 64 } && scope.PayloadFingerprint.All(char.IsAsciiHexDigit)
            && new[] { scope.TenantId, scope.ResourceId, scope.Method, scope.Contract, scope.DigestKeyVersion, scope.AuthenticatedTargetTenantId }.All(ValidText);
    private static bool ValidCredentialIdentities(PrivateOwnerOperationCredential c)
        => ValidScope(c.Scope) && new[] { c.ProfileVersion, c.Issuer, c.Audience, c.MachineIssuer, c.MachineSubject, c.MachineClient,
            c.MachineAudience, c.AuthorityReference, c.SigningKeyVersion }.All(ValidText);''')
s=s.replace('''        if (scope.PayloadFingerprint is not { Length: 64 } || scope.PayloadFingerprint.Any(c => !char.IsAsciiHexDigit(c))) { throw new ArgumentException("Invalid private keyed request fingerprint."); }
        foreach (string field in new[] { scope.TenantId, scope.ResourceId, scope.Method, scope.Contract, scope.DigestKeyVersion, scope.AuthenticatedTargetTenantId })
        { ArgumentException.ThrowIfNullOrWhiteSpace(field); }''','''        if (!ValidScope(scope)) { throw new ArgumentException("Invalid bounded private owner scope."); }''')
s=s.replace('if (result.Status == CustodyStatus.Succeeded) { return result.Key; }', 'if (result.Status == CustodyStatus.Succeeded && result.Key is not null && ValidText(result.Key.Metadata.Version)) { return result.Key; }')
p.write_bytes(s.replace('\n','\r\n').encode())
