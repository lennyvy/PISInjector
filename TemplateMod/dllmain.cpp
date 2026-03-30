// =============================================================================
// PIS Template Mod – dllmain.cpp
// =============================================================================
//
// Dies ist eine Vorlage für eine PIS-Mod-DLL. Sie wird vom PISModLoader
// per DLL-Injection in den Spielprozess (LortGame-Win64-Shipping.exe) geladen.
//
// ABLAUF:
//   1. PISInjector.exe erkennt die DLL im "mods" Ordner und zeigt sie in der Liste an.
//      Mod-Infos werden gelesen aus (in dieser Reihenfolge):
//        a) TemplateMod.json    – JSON-Datei neben der DLL (höchste Priorität)
//        b) VERSIONINFO         – In die DLL kompiliert (aus resource.h / TemplateMod.rc)
//        c) Dateiname           – Fallback wenn nichts anderes vorhanden
//   2. Der User klickt "Inject & Load Mods".
//   3. PISModLoader.dll injiziert diese DLL in den laufenden Spielprozess.
//   4. Windows ruft automatisch DllMain() mit DLL_PROCESS_ATTACH auf.
//   5. Ab hier läuft dein Mod-Code IM Spielprozess.
//
// WICHTIG:
//   - Diese DLL läuft NICHT im Launcher, sondern im Spiel!
//   - DllMain sollte so kurz wie möglich sein (keine langen Operationen).
//   - Für aufwendige Logik einen eigenen Thread starten (siehe unten).
//
// BAUEN:
//   - Visual Studio > Neues Projekt > "Dynamic-Link Library (DLL)" (C++)
//   - Plattform: x64 (muss zum Spiel passen)
//   - Dateien: dllmain.cpp, resource.h, TemplateMod.rc
//   - Release bauen, die .dll in den "mods" Ordner kopieren.
//   - JSON optional daneben legen (überschreibt VERSIONINFO im Launcher).
//
// =============================================================================

#include <Windows.h>
#include <cstdio>
#include "resource.h"       // MOD_NAME, MOD_VERSION_STR etc. (für VERSIONINFO)

// =============================================================================
// Vorwärtsdeklaration: Dein eigentlicher Mod-Code (läuft in einem eigenen Thread)
// =============================================================================
DWORD WINAPI ModMain(LPVOID lpParameter);

// =============================================================================
// DllMain – Einstiegspunkt der DLL
// =============================================================================
// Wird von Windows aufgerufen, sobald die DLL in einen Prozess geladen wird.
// ACHTUNG: In DllMain darf man NICHT:
//   - Andere DLLs laden (LoadLibrary)
//   - Auf Synchronisationsobjekte warten
//   - Lange Operationen ausführen
// Deshalb starten wir hier nur einen Thread und kehren sofort zurück.
// =============================================================================
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        // DLL wurde in den Spielprozess geladen.
        // DisableThreadLibraryCalls: Wir brauchen keine Thread-Attach/Detach Benachrichtigungen.
        DisableThreadLibraryCalls(hModule);

        // Eigenen Thread starten, damit DllMain sofort zurückkehren kann.
        // hModule wird als Parameter übergeben, falls wir es später brauchen
        // (z.B. um den Pfad der DLL zu ermitteln oder FreeLibraryAndExitThread aufzurufen).
        CreateThread(nullptr, 0, ModMain, hModule, 0, nullptr);
        break;

    case DLL_PROCESS_DETACH:
        // DLL wird entladen (Spiel beendet sich oder DLL wird manuell entladen).
        // Hier Cleanup machen: Hooks entfernen, Speicher freigeben, etc.
        break;
    }

    return TRUE;
}

// =============================================================================
// ModMain – Dein Mod-Code (eigener Thread)
// =============================================================================
// Hier kannst du alles machen was du willst:
//   - Speicher des Spiels lesen/schreiben
//   - Funktionen hooken
//   - Eigene Fenster erstellen
//   - Dateien lesen/schreiben
//   - usw.
// =============================================================================
DWORD WINAPI ModMain(LPVOID lpParameter)
{
    // Das hModule der DLL – übergeben von DllMain
    HMODULE hModule = static_cast<HMODULE>(lpParameter);

    // --- Beispiel: Debug-Konsole öffnen (hilfreich zum Entwickeln) ---
    AllocConsole();                                     // Neue Konsole erstellen
    FILE* console = nullptr;
    freopen_s(&console, "CONOUT$", "w", stdout);        // stdout auf die Konsole umleiten

    printf("[TemplateMod] Mod geladen! PID: %lu\n", GetCurrentProcessId());
    printf("[TemplateMod] DLL-Handle: 0x%p\n", hModule);
    printf("[TemplateMod] Basisadresse des Spiels: 0x%p\n", GetModuleHandle(nullptr));

    // =================================================================
    // AB HIER: Dein eigentlicher Mod-Code
    // =================================================================
    //
    // Beispiele was du hier machen könntest:
    //
    // 1) Speicher lesen:
    //    uintptr_t baseAddr = (uintptr_t)GetModuleHandle(nullptr);
    //    int health = *(int*)(baseAddr + 0x12345);
    //
    // 2) Speicher schreiben:
    //    *(int*)(baseAddr + 0x12345) = 9999;
    //
    // 3) Funktion hooken (z.B. mit MinHook):
    //    MH_CreateHook(targetFunc, hookedFunc, &originalFunc);
    //
    // 4) Endlosschleife für kontinuierliche Aktionen:
    //    while (true) { ... Sleep(100); }
    //
    // =================================================================

    // Beispiel: Einfache Schleife die zeigt, dass der Mod läuft
    for (int i = 0; i < 10; i++)
    {
        printf("[TemplateMod] Tick %d/10\n", i + 1);
        Sleep(1000);    // 1 Sekunde warten
    }

    printf("[TemplateMod] Mod fertig. Konsole bleibt offen.\n");

    // Wenn der Mod fertig ist und sich selbst entladen soll:
    // FreeLibraryAndExitThread(hModule, 0);
    // ACHTUNG: Nach FreeLibraryAndExitThread darf KEIN Code mehr kommen!

    return 0;
}
