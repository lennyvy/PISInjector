// =============================================================================
// resource.h – Versionsinformationen that gets compiled into DLL
// =============================================================================
// This Defines get used for TemplateMod.rc, to embed the VERSIONINFO-
// Resource into the DLL. The PISInjector read this automaticlly.
//
// CUSTOMIZE: Chang the Values here and they go straight into the compiled DLL.
//            No separate JSON-Datei needed (could be used if needed).
// =============================================================================

#pragma once

// --- Version as numbers (Major, Minor, Patch, Build) ---
#define MOD_VERSION_MAJOR   1
#define MOD_VERSION_MINOR   0
#define MOD_VERSION_PATCH   0
#define MOD_VERSION_BUILD   0

// --- Mod-Informationen as Strings ---
#define MOD_NAME            "Template Mod"
#define MOD_VERSION_STR     "1.0.0"
#define MOD_AUTHOR          "YourName"
#define MOD_DESCRIPTION     "A TemplateMod for the PISInjector"
