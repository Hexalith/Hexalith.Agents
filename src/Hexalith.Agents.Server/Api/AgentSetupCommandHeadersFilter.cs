namespace Hexalith.Agents.Server.Api;

using System.Globalization;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

/// <summary>
/// Validates caller-supplied setup command identities before an administration operation is invoked.
/// </summary>
internal sealed class AgentSetupCommandHeadersFilter : IEndpointFilter
{
    internal const string CorrelationHeaderName = "X-Correlation-ID";
    internal const string IdempotencyHeaderName = "Idempotency-Key";
    internal const string ExpectedConfigurationVersionHeaderName = "X-Expected-Configuration-Version";
    internal const string InvalidIdentityProblemType = "https://hexalith.com/problems/agents/invalid-command-identity";
    internal const string InvalidActivationVersionProblemType = "https://hexalith.com/problems/agents/invalid-activation-version";

    private readonly bool _requireActivationVersion;

    /// <summary>
    /// Initializes a filter for a setup write that does not require an activation version.
    /// </summary>
    public AgentSetupCommandHeadersFilter()
        : this(requireActivationVersion: false)
    {
    }

    private AgentSetupCommandHeadersFilter(bool requireActivationVersion)
    {
        _requireActivationVersion = requireActivationVersion;
    }

    /// <summary>
    /// Creates the activation-specific filter that also requires a positive canonical configuration version.
    /// </summary>
    /// <returns>The activation filter.</returns>
    internal static AgentSetupCommandHeadersFilter ForActivation()
        => new(requireActivationVersion: true);

    /// <inheritdoc />
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IHeaderDictionary headers = context.HttpContext.Request.Headers;
        IResult? identityFailure = ValidateOptionalUlid(headers, CorrelationHeaderName)
            ?? ValidateOptionalUlid(headers, IdempotencyHeaderName);
        if (identityFailure is not null)
        {
            return ValueTask.FromResult<object?>(identityFailure);
        }

        if (_requireActivationVersion
            && (!headers.TryGetValue(ExpectedConfigurationVersionHeaderName, out StringValues values)
                || values.Count != 1
                || !IsCanonicalPositiveInt32(values[0])))
        {
            return ValueTask.FromResult<object?>(Problem(
                InvalidActivationVersionProblemType,
                "Invalid activation configuration version",
                ExpectedConfigurationVersionHeaderName,
                "A single positive invariant-decimal configuration version is required."));
        }

        return next(context);
    }

    private static IResult? ValidateOptionalUlid(IHeaderDictionary headers, string headerName)
    {
        if (!headers.TryGetValue(headerName, out StringValues values))
        {
            return null;
        }

        string? value = values.Count == 1 ? values[0] : null;
        if (value is not null
            && NUlid.Ulid.TryParse(value, out NUlid.Ulid parsed)
            && string.Equals(parsed.ToString(), value, StringComparison.Ordinal))
        {
            return null;
        }

        return Problem(
            InvalidIdentityProblemType,
            "Invalid command identity",
            headerName,
            "When supplied, the header must contain one canonical uppercase ULID.");
    }

    private static bool IsCanonicalPositiveInt32(string? value)
        => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)
            && parsed > 0
            && string.Equals(parsed.ToString(CultureInfo.InvariantCulture), value, StringComparison.Ordinal);

    private static IResult Problem(string type, string title, string headerName, string detail)
        => Results.Problem(
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            title: title,
            type: type,
            extensions: new Dictionary<string, object?>
            {
                ["header"] = headerName,
                ["reasonCode"] = "canonical_value_required",
            });
}
