# PIS Mod Launcher

A lightweight WPF application that discovers, lists, and injects native mod DLLs
into the **LortGame** (`LortGame-Win64-Shipping.exe`) process.

Everything ships as a **single self-contained `.exe`** – no external runtime or
helper DLL required.

## Features

- **Automatic mod discovery** – drop `.dll` files into the `mods/` folder and
  they appear in the launcher.
- **Rich metadata** – mod name, version, author, and description are read from:
  1. A JSON sidecar file (`MyMod.json`) – highest priority
  2. The VERSIONINFO resource embedded in the DLL – automatic
  3. The filename – fallback
- **One-click injection** – select which mods to load, click *Inject & Load Mods*,
  and the launcher waits for the game process then injects every selected DLL via
  `CreateRemoteThread` + `LoadLibraryW`.
- **Per-DLL status reporting** – each mod gets a clear OK / error status in the
  log panel.

## Project Structure

```
PISInjector/
├── App.xaml / App.xaml.cs        # Application entry point & global error handling
├── MainWindow.xaml / .xaml.cs    # UI: mod list, inject button, log panel
├── ProcessInjector.cs            # Managed DLL injection (Win32 P/Invoke)
├── ModInfo.cs                    # View-model for a single mod entry
├── ModConfig.cs                  # JSON schema for mod sidecar config files
├── AssemblyInfo.cs               # WPF theme info
├── PISInjector.csproj            # Build config (single-file, self-contained)
└── TemplateMod/                  # Starter template for creating new C++ mods
    └── README.md
```

## How It Works

1. On startup the launcher scans `mods/` for `.dll` files and reads their
   metadata (JSON → VERSIONINFO → filename).
2. The user checks which mods to inject and clicks **Inject & Load Mods**.
3. `ProcessInjector` polls for `LortGame-Win64-Shipping.exe` (7 s timeout).
4. For each selected DLL:
   - `VirtualAllocEx` allocates memory in the game process.
   - `WriteProcessMemory` writes the DLL path (UTF-16) into that memory.
   - `CreateRemoteThread` starts a remote thread calling `LoadLibraryW`.
   - The thread's exit code is checked – a non-zero `HMODULE` means success.
5. Results are displayed per mod in the log panel.

## Building

**Prerequisites:** .NET 10 SDK, Visual Studio 2022+ with the **.NET desktop
development** workload.

```bash
# Debug build
dotnet build

# Release build (auto-publishes a single-file .exe)
dotnet build -c Release
```

The published executable is placed in `bin/Release/net10.0-windows/win-x64/publish/`.

## Usage

```
PISInjector.exe
mods/
  ItemSpawner.dll          ← Your mod DLL
  ItemSpawner.json         ← Optional: overrides metadata shown in the launcher
```

1. Place mod DLLs (and optional `.json` configs) in the `mods/` folder next to
   the executable.
2. Launch **PISInjector.exe**.
3. Check the mods you want to load.
4. Start the game.
5. Click **Inject & Load Mods**.

## Creating Mods

See [`TemplateMod/README.md`](TemplateMod/README.md) for a step-by-step guide
on creating a new C++ mod DLL that works with this launcher.

## License

This project is provided as-is for educational and modding purposes.
