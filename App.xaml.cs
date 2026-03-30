using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace PISModLauncher;

/// <summary>
/// Interaction logic for App.xaml.
/// Registers global exception handlers that write to <c>PISInjector_ERROR.log</c>
/// so crashes can be diagnosed even when no window is visible.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Directory where the actual .exe resides.
    /// Uses <c>Environment.ProcessPath</c> for single-file apps,
    /// falls back to <c>AppDomain.CurrentDomain.BaseDirectory</c>.
    /// </summary>
    internal static string ExeDirectory { get; } = ResolveExeDirectory();

    /// <summary>Path to the error log file next to the executable.</summary>
    private static readonly string LogPath =
        Path.Combine(ExeDirectory, "PISInjector_ERROR.log");

    /// <summary>
    /// Determines the directory containing the running executable.
    /// Prefers <see cref="Environment.ProcessPath"/> (works correctly for
    /// single-file published apps) and falls back to
    /// <see cref="AppDomain.CurrentDomain.BaseDirectory"/>.
    /// </summary>
    private static string ResolveExeDirectory()
    {
        try
        {
            string? processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
            {
                string? dir = Path.GetDirectoryName(processPath);
                if (!string.IsNullOrEmpty(dir))
                    return dir;
            }
        }
        catch
        {
            // Swallow – fall through to fallback
        }

        return AppDomain.CurrentDomain.BaseDirectory;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Catch unhandled exceptions on the UI thread
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Catch unhandled exceptions on background threads
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Write a startup marker so we know the app at least got this far
        WriteLog("Startup", $"ExeDirectory = {ExeDirectory}");

        base.OnStartup(e);
    }

    /// <summary>
    /// Handles unhandled exceptions on the WPF dispatcher (UI) thread.
    /// Logs the exception and shows a message box so the user knows what happened.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteLog("DispatcherUnhandledException", e.Exception);
        e.Handled = true;
        MessageBox.Show(
            "Unexpected error – see PISInjector_ERROR.log\n\n" + e.Exception.Message,
            "PIS Mod Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// Handles unhandled exceptions on non-UI (background / thread-pool) threads.
    /// Only logs; no message box because the UI may not be available.
    /// </summary>
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            WriteLog("AppDomain.UnhandledException", ex);
    }

    /// <summary>
    /// Appends a timestamped error entry to <see cref="LogPath"/>.
    /// </summary>
    internal static void WriteLog(string source, Exception ex)
    {
        WriteLog(source, ex.ToString());
    }

    /// <summary>
    /// Appends a timestamped text entry to <see cref="LogPath"/>.
    /// </summary>
    internal static void WriteLog(string source, string message)
    {
        try
        {
            string entry =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]{Environment.NewLine}" +
                $"{message}{Environment.NewLine}" +
                $"---{Environment.NewLine}";
            File.AppendAllText(LogPath, entry);
        }
        catch
        {
            // Logging itself must never throw
        }
    }
}

