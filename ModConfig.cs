namespace PISModLauncher;

/// <summary>
/// JSON schema for the mod configuration file (e.g. <c>MyMod.json</c>).
/// Modders place this file next to their DLL to provide metadata
/// that the launcher displays in the mod list.
/// </summary>
/// <example>
/// <code>
/// {
///   "Name": "My Cool Mod",
///   "Version": "1.0.0",
///   "Author": "ModderName",
///   "Description": "Adds cool features to the game."
/// }
/// </code>
/// </example>
public class ModConfig
{
    /// <summary>Display name shown in the launcher. Overrides VERSIONINFO ProductName.</summary>
    public string? Name { get; set; }

    /// <summary>Version string shown in the launcher. Overrides VERSIONINFO ProductVersion.</summary>
    public string? Version { get; set; }

    /// <summary>Author name shown in the launcher. Overrides VERSIONINFO CompanyName.</summary>
    public string? Author { get; set; }

    /// <summary>Short description shown in the launcher. Overrides VERSIONINFO FileDescription.</summary>
    public string? Description { get; set; }
}
