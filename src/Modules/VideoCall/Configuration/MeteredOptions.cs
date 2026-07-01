namespace LinkUp.Modules.VideoCall.Configuration;

/// <summary>
/// Metered managed TURN settings (bound from the "Metered" config section).
/// Kept server-side so the API key is never shipped to the browser.
/// Leave <see cref="ApiKey"/> empty to run STUN-only (no TURN fallback).
/// </summary>
public class MeteredOptions
{
    public const string SectionName = "Metered";

    /// <summary>App subdomain, e.g. "linkup" → https://linkup.metered.live.</summary>
    public string AppName { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
