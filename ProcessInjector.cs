using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PISModLauncher;

/// <summary>
/// Injects DLLs into a remote process using the classic
/// <c>CreateRemoteThread</c> + <c>LoadLibraryW</c> technique.
/// Entirely managed – no external native helper DLL required.
/// </summary>
internal static partial class ProcessInjector
{
    // ── Status codes returned per DLL ────────────────────────────────

    /// <summary>Injection succeeded – the mod DLL is now loaded in the target process.</summary>
    public const int ResultOk = 0;
    /// <summary>The target process was not found within the timeout period.</summary>
    public const int ResultProcessNotFound = 1;
    /// <summary>OpenProcess failed (insufficient privileges or invalid PID).</summary>
    public const int ResultOpenProcessFailed = 2;
    /// <summary>VirtualAllocEx failed to allocate memory in the target process.</summary>
    public const int ResultAllocFailed = 3;
    /// <summary>WriteProcessMemory failed to write the DLL path into target memory.</summary>
    public const int ResultWriteFailed = 4;
    /// <summary>CreateRemoteThread failed to start the remote LoadLibraryW call.</summary>
    public const int ResultRemoteThreadFailed = 5;
    /// <summary>The remote thread did not finish within the per-DLL timeout.</summary>
    public const int ResultTimeout = 6;
    /// <summary>LoadLibraryW returned NULL inside the target process (DLL not found or init failed).</summary>
    public const int ResultLoadLibraryFailed = 7;

    // ── Win32 constants ──────────────────────────────────────────────
    private const uint PROCESS_CREATE_THREAD = 0x0002;
    private const uint PROCESS_VM_OPERATION = 0x0008;
    private const uint PROCESS_VM_WRITE = 0x0020;
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;

    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint MEM_RELEASE = 0x8000;
    private const uint PAGE_READWRITE = 0x04;

    private const uint INFINITE = 0xFFFFFFFF;
    private const uint WAIT_OBJECT_0 = 0x00000000;
    private const uint WAIT_TIMEOUT = 0x00000102;

    // ── P/Invoke declarations ────────────────────────────────────────

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint hObject);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint VirtualAllocEx(nint hProcess, nint lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualFreeEx(nint hProcess, nint lpAddress, nuint dwSize, uint dwFreeType);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WriteProcessMemory(nint hProcess, nint lpBaseAddress, byte[] lpBuffer, nuint nSize, out nuint lpNumberOfBytesWritten);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint CreateRemoteThread(nint hProcess, nint lpThreadAttributes, nuint dwStackSize, nint lpStartAddress, nint lpParameter, uint dwCreationFlags, out uint lpThreadId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetExitCodeThread(nint hThread, out uint lpExitCode);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial nint GetModuleHandleW(string lpModuleName);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    private static partial nint GetProcAddress(nint hModule, string lpProcName);

    // ── Public API ───────────────────────────────────────────────────

    /// <summary>
    /// Waits for the target process and injects every given DLL into it.
    /// </summary>
    /// <param name="processName">
    /// Name of the target process, e.g. <c>"LortGame-Win64-Shipping.exe"</c>.
    /// The <c>.exe</c> extension is stripped automatically for
    /// <see cref="Process.GetProcessesByName"/>.
    /// </param>
    /// <param name="dllPaths">Full paths of DLLs to inject.</param>
    /// <param name="timeoutMs">
    /// Maximum time in milliseconds to wait for the target process to appear.
    /// </param>
    /// <param name="results">
    /// Output array (same length as <paramref name="dllPaths"/>).
    /// Each element receives one of the <c>Result*</c> status codes.
    /// </param>
    /// <returns>Number of DLLs that were injected successfully.</returns>
    public static int InjectMods(string processName, string[] dllPaths, int timeoutMs, int[] results)
    {
        // Resolve LoadLibraryW address once – it is at the same virtual
        // address in every process thanks to kernel32.dll base sharing.
        nint kernel32 = GetModuleHandleW("kernel32.dll");
        nint loadLibraryAddr = GetProcAddress(kernel32, "LoadLibraryW");
        if (loadLibraryAddr == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetProcAddress(LoadLibraryW) failed.");

        // Strip ".exe" for Process.GetProcessesByName
        string nameWithoutExt = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName[..^4]
            : processName;

        // Wait for the target process to appear
        Process? target = WaitForProcess(nameWithoutExt, timeoutMs);
        if (target is null)
        {
            Array.Fill(results, ResultProcessNotFound);
            return 0;
        }

        // Open the target process with the required access rights
        nint hProcess = OpenProcess(
            PROCESS_CREATE_THREAD | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ | PROCESS_QUERY_INFORMATION,
            false,
            target.Id);

        if (hProcess == 0)
        {
            Array.Fill(results, ResultOpenProcessFailed);
            return 0;
        }

        try
        {
            int loaded = 0;
            for (int i = 0; i < dllPaths.Length; i++)
            {
                results[i] = InjectSingleDll(hProcess, loadLibraryAddr, dllPaths[i]);
                if (results[i] == ResultOk)
                    loaded++;
            }
            return loaded;
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────

    /// <summary>
    /// Polls <see cref="Process.GetProcessesByName"/> every 250 ms until a process
    /// with the given name appears or <paramref name="timeoutMs"/> elapses.
    /// </summary>
    /// <param name="nameWithoutExt">Process name without the <c>.exe</c> extension.</param>
    /// <param name="timeoutMs">Maximum wait time in milliseconds.</param>
    /// <returns>The first matching <see cref="Process"/>, or <c>null</c> on timeout.</returns>
    private static Process? WaitForProcess(string nameWithoutExt, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            Process[] procs = Process.GetProcessesByName(nameWithoutExt);
            if (procs.Length > 0)
                return procs[0];

            System.Threading.Thread.Sleep(250);
        }
        return null;
    }

    /// <summary>
    /// Injects a single DLL into an already-opened process by writing its path
    /// into remote memory and spawning a thread that calls <c>LoadLibraryW</c>.
    /// </summary>
    /// <param name="hProcess">Handle to the target process (must have VM + thread creation rights).</param>
    /// <param name="loadLibraryAddr">Address of <c>LoadLibraryW</c> in kernel32.dll.</param>
    /// <param name="dllPath">Absolute path of the DLL to inject.</param>
    /// <returns>One of the <c>Result*</c> status codes.</returns>
    private static int InjectSingleDll(nint hProcess, nint loadLibraryAddr, string dllPath)
    {
        // Encode the DLL path as a null-terminated UTF-16 string
        byte[] pathBytes = Encoding.Unicode.GetBytes(dllPath + '\0');
        nuint pathSize = (nuint)pathBytes.Length;

        // Allocate memory inside the target process
        nint remoteMem = VirtualAllocEx(hProcess, 0, pathSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (remoteMem == 0)
            return ResultAllocFailed;

        try
        {
            // Write the DLL path into the allocated memory
            if (!WriteProcessMemory(hProcess, remoteMem, pathBytes, pathSize, out _))
                return ResultWriteFailed;

            // Create a remote thread that calls LoadLibraryW(remoteMem)
            nint hThread = CreateRemoteThread(hProcess, 0, 0, loadLibraryAddr, remoteMem, 0, out _);
            if (hThread == 0)
                return ResultRemoteThreadFailed;

            try
            {
                // Wait for LoadLibraryW to finish (max 10 seconds per DLL)
                uint waitResult = WaitForSingleObject(hThread, 10_000);
                if (waitResult == WAIT_TIMEOUT)
                    return ResultTimeout;

                // Check the exit code – it is the HMODULE returned by LoadLibraryW.
                // A zero value means LoadLibraryW failed inside the target process.
                if (GetExitCodeThread(hThread, out uint exitCode) && exitCode == 0)
                    return ResultLoadLibraryFailed;

                return ResultOk;
            }
            finally
            {
                CloseHandle(hThread);
            }
        }
        finally
        {
            VirtualFreeEx(hProcess, remoteMem, 0, MEM_RELEASE);
        }
    }
}
