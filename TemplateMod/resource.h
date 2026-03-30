// =============================================================================
// resource.h – Versionsinformationen die in die DLL kompiliert werden
// =============================================================================
// Diese Defines werden von TemplateMod.rc benutzt, um die VERSIONINFO-
// Resource in die DLL einzubetten. Der PISInjector liest diese automatisch.
//
// ANPASSEN: Ändere die Werte hier, und sie landen direkt in der fertigen DLL.
//           Keine separate JSON-Datei nötig (kann aber zusätzlich genutzt werden).
// =============================================================================

#pragma once

// --- Version als Zahlen (Major, Minor, Patch, Build) ---
#define MOD_VERSION_MAJOR   1
#define MOD_VERSION_MINOR   0
#define MOD_VERSION_PATCH   0
#define MOD_VERSION_BUILD   0

// --- Mod-Informationen als Strings ---
#define MOD_NAME            "Template Mod"
#define MOD_VERSION_STR     "1.0.0"
#define MOD_AUTHOR          "DeinName"
#define MOD_DESCRIPTION     "Eine Vorlage die zeigt wie ein PIS-Mod aufgebaut ist."
