// =============================================================================
// PISInjector Template Mod – dllmain.cpp
// =============================================================================
//
// This is a template mod for the PISInjector. This .dll gets injected via PISInjector into the 
// (LortGame-Win64-Shipping.exe) game process.
//
// Process:
//   1. PISInjector.exe registers the mods straight from the "mods" folder and show it in the programm.
//      Mod-Infos are read in in this order:
//        a) TemplateMod.json    – JSON-File next to the DLL (highest priority)
//        b) VERSIONINFO         – gets compiled in the .dll file (from resource.h / TemplateMod.rc)
//        c) Dateiname           – Fallback if nothing else is presented
//   2. The user presses "Inject & Load Mods".
//   3. PISModLoader.dll injects this .dll into the running Gameprocess.
//   4. Windows calls the DllMain() automaticlly with DLL_PROCESS_ATTACH.
//   5. Then your mod code runs.
//
// IMPORTANT:
//   - This DLL does NOT run in the launcher itself, but in the game process!
//   - DllMain should be as short as possible (no long operations).
//   - To create more complex code create a new thread (view below).
//
// BUILD:
//   - Visual Studio (recommended) > new project > "Dynamic-Link Library (DLL)" (C++)
//   - Plattform: x64 (must match the game)
//   - Files: dllmain.cpp, resource.h, TemplateMod.rc
//   - Build as release, theh copy .dll to the "mods" folder of the PISInjector.
//   - Put your JSON optional next to the .dll (overrides VERSIONINFO in the Launcher).
//
// =============================================================================

#include <Windows.h>
#include <cstdio>
#include "resource.h"       // MOD_NAME, MOD_VERSION_STR etc. (for your VERSIONINFO)

// =============================================================================
// Fronterdeclaration: Your actual Mod-Code (runs in its own Thread)
// =============================================================================
DWORD WINAPI ModMain(LPVOID lpParameter);

// =============================================================================
// DllMain – Entry-Point of the DLL
// =============================================================================
// Gets calld by Windows, as soon as the DLL gets loaded into the process.
// IMPORTANT: In your DllMain do NOT:
//   - Load other DLLs (LoadLibrary)
//   - Wait for sync objects
//   - execute  long operations
// Thats why we just start a thread here and go back after
// =============================================================================
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        // DLL gets loaded into the game process.
        // DisableThreadLibraryCalls: We don't need a Thread-Attach/Detach notifiation.
        DisableThreadLibraryCalls(hModule);

        // Start own Thread, so your gets back DllMain instantly.
        // hModule gets transfered as parameter, if needed later
        // (for example to find the path of your DLL or to call FreeLibraryAndExitThread).
        CreateThread(nullptr, 0, ModMain, hModule, 0, nullptr);
        break;

    case DLL_PROCESS_DETACH:
        // DLL gets unloaded (Game process closes or DLL gets manually unloaded).
        // Here: Cleanup (Remove Hooks, Free Disk, etc.)
        break;
    }

    return TRUE;
}

// =============================================================================
// ModMain – Your Mod-Code (own Thread)
// =============================================================================
// Here you can do whatever you want:
//   - Read/Write memory of the game
//   - function hooks
//   - create your own window
//   - Read/Write Files
//   - ...
// =============================================================================
DWORD WINAPI ModMain(LPVOID lpParameter)
{
    // The hModule of your DLL – transfered from DllMain
    HMODULE hModule = static_cast<HMODULE>(lpParameter);

    // --- Example: Debug-Console opened (VERY HELPFULL BTW ;-;) ---
    AllocConsole();                                     // creates new console
    FILE* console = nullptr;
    freopen_s(&console, "CONOUT$", "w", stdout);        // stdout transmitted to console

    printf("[TemplateMod] Mod geladen! PID: %lu\n", GetCurrentProcessId());
    printf("[TemplateMod] DLL-Handle: 0x%p\n", hModule);
    printf("[TemplateMod] Basisadresse des Spiels: 0x%p\n", GetModuleHandle(nullptr));

    // =================================================================
    // From here: your actual own Mod-Code
    // =================================================================
    //
    // Some small examples to get to know the enviroment:
    //
    // 1) Read Memory:
    //    uintptr_t baseAddr = (uintptr_t)GetModuleHandle(nullptr);
    //    int health = *(int*)(baseAddr + 0x12345);
    //
    // 2) Write Memory:
    //    *(int*)(baseAddr + 0x12345) = 9999;
    //
    // 3) Hook function (MinHook recommended):
    //    MH_CreateHook(targetFunc, hookedFunc, &originalFunc);
    //
    // 4) Endlessloop for continuing actions:
    //    while (true) { ... Sleep(100); }
    //
    // =================================================================

    // Example: Simple Loop that shows, that your mod runs
    for (int i = 0; i < 10; i++)
    {
        printf("[TemplateMod] Tick %d/10\n", i + 1);
        Sleep(1000);    // 1 Sekunde warten
    }

    printf("[TemplateMod] Mod ready. Console stays opened.\n");

    // If Mod is done and should unload itself:
    // FreeLibraryAndExitThread(hModule, 0);
    // ATTENTION: After FreeLibraryAndExitThread should come NOTHING MORE!

    return 0;
}
