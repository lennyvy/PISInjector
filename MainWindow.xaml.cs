using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace PISModLauncher
{
    /// <summary>
    /// Main window of the PIS Mod Launcher.
    /// Displays available mods from the "mods" folder and allows the user
    /// to select which ones to inject into the target game process.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>List of all discovered mods, bound to the UI ListView.</summary>
        private readonly List<ModInfo> mods = new List<ModInfo>();

        /// <summary>JSON options used when reading mod config files.</summary>
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        /// <summary>
        /// Initializes the window and populates the mod list from disk.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                LoadModList();
            }
            catch (Exception ex)
            {
                App.WriteLog("MainWindow.ctor", ex);
                Log("Failed to load mod list – see PISInjector_ERROR.log");
            }
        }

        /// <summary>
        /// Scans the "mods" folder next to the executable for DLL files,
        /// reads their metadata with the following priority:
        /// <list type="number">
        ///   <item>JSON config file (e.g. <c>MyMod.json</c>) – explicit override</item>
        ///   <item>VERSIONINFO resource compiled into the DLL – automatic</item>
        ///   <item>Filename – fallback</item>
        /// </list>
        /// </summary>
        private void LoadModList()
        {
            mods.Clear();

            string modsFolder = Path.Combine(App.ExeDirectory, "mods");

            // Create the folder on first launch so the user knows where to place mods
            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
                Log("Mods folder created: " + modsFolder);
                ModsList.ItemsSource = mods;
                return;
            }

            foreach (string dllPath in Directory.GetFiles(modsFolder, "*.dll"))
            {
                string fileName = Path.GetFileName(dllPath);
                string baseName = Path.GetFileNameWithoutExtension(dllPath);

                // Defaults – overwritten by JSON or VERSIONINFO
                string name = baseName;
                string version = "?";
                string author = "?";
                string description = "";
                bool metadataFound = false;

                // --- 1) JSON config (highest priority) ---
                string jsonPath = Path.ChangeExtension(dllPath, ".json");
                if (System.IO.File.Exists(jsonPath))
                {
                    try
                    {
                        string json = System.IO.File.ReadAllText(jsonPath);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var config = JsonSerializer.Deserialize<ModConfig>(json, JsonOptions);
                            if (config != null)
                            {
                                name        = config.Name ?? name;
                                version     = config.Version ?? version;
                                author      = config.Author ?? author;
                                description = config.Description ?? description;
                                metadataFound = true;
                            }
                        }
                        else
                        {
                            Log($"{baseName}.json is empty.");
                        }
                    }
                    catch (Exception ex)
                    {
                        App.WriteLog($"LoadModList JSON ({fileName})", ex);
                        Log($"{baseName}.json: Error – {ex.Message}");
                    }
                }

                // --- 2) VERSIONINFO embedded in the DLL (safe – does not trigger DllMain) ---
                if (!metadataFound)
                {
                    try
                    {
                        var vi = FileVersionInfo.GetVersionInfo(dllPath);

                        if (!string.IsNullOrWhiteSpace(vi.ProductName))
                            name = vi.ProductName;
                        if (!string.IsNullOrWhiteSpace(vi.ProductVersion))
                            version = vi.ProductVersion;
                        if (!string.IsNullOrWhiteSpace(vi.CompanyName))
                            author = vi.CompanyName;
                        if (!string.IsNullOrWhiteSpace(vi.FileDescription))
                            description = vi.FileDescription;
                    }
                    catch (Exception ex)
                    {
                        App.WriteLog($"LoadModList VERSIONINFO ({fileName})", ex);
                    }
                }

                // --- 3) Fallback: the filename is used as the display name (already set as default) ---

                mods.Add(new ModInfo
                {
                    Name = name,
                    Version = version,
                    Author = author,
                    Description = description,
                    File = fileName,
                    Enabled = true
                });
            }

            ModsList.ItemsSource = mods;
            Log($"{mods.Count} mod(s) found.");
        }

        /// <summary>
        /// Click handler for the "Inject &amp; Load Mods" button.
        /// Collects all checked mods and injects them into the target game process.
        /// </summary>
        private void Inject_Click(object sender, RoutedEventArgs e)
        {
            string modsFolder = Path.Combine(App.ExeDirectory, "mods");

            // Build an array of full paths for every checked mod
            string[] selectedPaths = mods
                .Where(m => m.Enabled)
                .Select(m => Path.Combine(modsFolder, m.File))
                .ToArray();

            if (selectedPaths.Length == 0)
            {
                MessageBox.Show("No mods selected.");
                return;
            }

            int[] results = new int[selectedPaths.Length];

            try
            {
                Log("Waiting for LortGame-Win64-Shipping.exe …");

                int loaded = ProcessInjector.InjectMods(
                    "LortGame-Win64-Shipping.exe",
                    selectedPaths,
                    7000,
                    results);

                Log($"{loaded} of {selectedPaths.Length} mod(s) loaded.");

                for (int i = 0; i < results.Length; i++)
                {
                    string status = results[i] switch
                    {
                        ProcessInjector.ResultOk                => "OK",
                        ProcessInjector.ResultProcessNotFound    => "Process not found",
                        ProcessInjector.ResultOpenProcessFailed  => "OpenProcess failed",
                        ProcessInjector.ResultAllocFailed        => "Memory allocation failed",
                        ProcessInjector.ResultWriteFailed        => "WriteProcessMemory failed",
                        ProcessInjector.ResultRemoteThreadFailed => "CreateRemoteThread failed",
                        ProcessInjector.ResultTimeout            => "Timeout",
                        ProcessInjector.ResultLoadLibraryFailed  => "LoadLibrary failed in target process",
                        _                                       => $"Unknown error ({results[i]})"
                    };
                    Log($"  {Path.GetFileName(selectedPaths[i])}: {status}");
                }

                if (loaded == 0 && results.Length > 0 && results[0] == ProcessInjector.ResultProcessNotFound)
                {
                    MessageBox.Show(
                        "LortGame-Win64-Shipping.exe was not found.\n\n" +
                        "Please start the game and click Inject again.",
                        "PIS Mod Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                App.WriteLog("Inject_Click", ex);
                MessageBox.Show("Failed to load mods:\n" + ex.Message);
                Log("Loading failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Appends a line to the log text box at the bottom of the window.
        /// </summary>
        /// <param name="text">The message to log.</param>
        private void Log(string text)
        {
            LogText.AppendText(text + Environment.NewLine);
            LogText.ScrollToEnd();
        }
    }
}