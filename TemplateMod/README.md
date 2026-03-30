# PIS Template Mod

A starter template for PIS mods that get injected into `LortGame-Win64-Shipping.exe`.

## Files

| File | Description |
|---|---|
| `dllmain.cpp` | The mod code. Contains `DllMain` (entry point) and `ModMain` (custom thread). |
| `resource.h` | Mod metadata (Name, Version, Author, Description) as defines – compiled into the DLL. |
| `TemplateMod.rc` | Windows resource file – embeds the defines from `resource.h` as VERSIONINFO into the DLL. |
| `TemplateMod.json` | **Optional**: JSON config placed next to the DLL. Overrides VERSIONINFO in the launcher. |
| `TemplateMod.def` | **Optional**: Module definition file for custom DLL exports. |

## Mod Metadata: Where Does the Launcher Read From?

| Priority | Source | Where to edit | Advantage |
|---|---|---|---|
| 1️⃣ | **JSON** (`MyMod.json`) | Text file next to the DLL | Can be changed without recompiling |
| 2️⃣ | **VERSIONINFO** (in the DLL) | `resource.h` → rebuild | Everything in one file, cannot be tampered with |
| 3️⃣ | **Filename** | Rename the DLL | Automatic fallback |

## Creating a New Mod Project

1. **Visual Studio** > New Project > **Dynamic-Link Library (DLL)** (C++)
2. Set the platform to **x64** (must match the game)
3. Add files to the project: `dllmain.cpp`, `resource.h`, `TemplateMod.rc`
4. Fill in your mod info in `resource.h`
5. Build **Release | x64**

## Installing a Mod

Just the `.dll` is enough – the metadata is embedded via VERSIONINFO:
```
PISInjector.exe
mods/
  MyMod.dll               ← Your mod DLL (contains VERSIONINFO)
  MyMod.json              ← Optional: overrides the DLL metadata in the launcher
```

## Tips

- **Debug console**: `AllocConsole()` + `freopen_s()` opens a CMD window in the game for debugging
- **Keep DllMain short**: Only start a thread – no heavy operations
- **Hooks**: For function hooking consider [MinHook](https://github.com/TsudaKageworker/minhook)
- **Memory addresses**: Use Cheat Engine or ReClass to find addresses in the game
