# Story 5.2 command identity migration

Setup-write callers may omit `X-Correlation-ID` and `Idempotency-Key` and let the Agents service mint them. When
either header is supplied, it must now be one canonical uppercase ULID. The activation route also requires
`X-Expected-Configuration-Version` with the positive decimal version displayed when the intent was created.

Before deploying this change, drain or inspect any unresolved setup attempts whose identities were minted by the
previous `Guid.ToString("n")` implementation. A legacy identifier is rejected with HTTP 400 after upgrade. Never
translate it into a new ULID under the same retry: that would create a different idempotency intent. The operator
must either finish the old attempt before upgrade or explicitly abandon it and begin a new attempt afterward.
