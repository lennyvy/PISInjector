/// <summary>
/// Represents a single mod entry in the launcher's mod list.
/// Populated from a JSON config file, the DLL's embedded VERSIONINFO resource,
/// or the filename as a last resort.
/// </summary>
public class ModInfo
{
    /// <summary>Display name of the mod (e.g. "Item Spawner").</summary>
    public string Name { get; set; } = "";

    /// <summary>Version string (e.g. "1.2.0"). Defaults to "?" when unknown.</summary>
    public string Version { get; set; } = "?";

    /// <summary>Author or company name. Defaults to "?" when unknown.</summary>
    public string Author { get; set; } = "?";

    /// <summary>Short description shown in the mod list.</summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Filename of the mod DLL relative to the <c>mods/</c> folder
    /// (e.g. "ItemSpawner.dll").
    /// </summary>
    public string File { get; set; } = "";

    /// <summary>
    /// Whether the user has checked this mod for injection.
    /// Bound to the checkbox in the UI via two-way data binding.
    /// </summary>
    public bool Enabled { get; set; } = true;
}