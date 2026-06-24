using System.Collections.Generic;
using System.Globalization;

using Hexalith.Agents.UI.Resources;

using Microsoft.Extensions.Localization;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Test localizer that returns each resource key verbatim as its value. A returned value equal to the requested
/// key proves the component resolved a single whole string through the localizer rather than concatenating runtime
/// fragments (UX-DR14 / AC5), and keeps component tests independent of the .resx contents.
/// </summary>
internal sealed class StubAgentsLocalizer : IStringLocalizer<AgentsResources>
{
    public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

    public LocalizedString this[string name, params object[] arguments]
        => new(name, string.Format(CultureInfo.InvariantCulture, name, arguments), resourceNotFound: false);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}
