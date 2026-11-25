# Exportar / importar teclas Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Exportar las teclas de una cuenta a un `.zip` elegido por el usuario e importarlas a cualquier cuenta.

**Architecture:** Lógica en `src/Core/KeyArchive.cs` (probada en `tests/CoreTests.cs`); UI en `MainWindow`/`ConfirmDialog` con diálogos nativos de Windows.

**Tech Stack:** C# 5, .NET Framework 4.8, WPF, `System.IO.Compression` (ZipArchive).

**Spec:** `docs/superpowers/specs/2026-11-24-exportar-importar-teclas-design.md`

## Global Constraints
- C# 5 solamente; textos en español; nada escrito dentro de Steam salvo el `.lst` de destino.
- Límite de tamaño de teclas: 1 MB (`KeyArchive.MaxKeysBytes = 1048576`).
- Sin atribución en commits.

## Review Focus
- Zip corrupto o que no es zip → mensaje, sin crash.
- Zip con el `.lst` dentro de una subcarpeta → se acepta (se busca por nombre de archivo).
- Usuario cancela "Guardar como" con la casilla marcada → no se copia/importa nada.
- Nombre de cuenta con caracteres no válidos para archivo → nombre sugerido saneado.
- Importar con Steam abierto tras confirmar → no se aplica.

---

### Task 1: KeyArchive (núcleo)

**Files:** Create `src/Core/KeyArchive.cs`; Modify `src/Core/KeyOps.cs` (helper compartido), `tests/CoreTests.cs`, `build.ps1` (`/r:System.IO.Compression.dll`).

**Interfaces (Produces):**
- `class KeyPackage { byte[] Keys; string SourceName; string SourceAccountId; string Date; string FilePath; }`
- `static class KeyArchive { const long MaxKeysBytes; static void Export(SteamAccount acc, string zipPath); static KeyPackage Load(string path); static void Apply(KeyPackage pkg, SteamAccount to); static string SuggestedFileName(SteamAccount acc, DateTime now); }`
- `KeyOps.WriteKeys(SteamAccount to, byte[] data)` (interno): valida carpeta, escribe, atributos normales, fecha actual. `CopyKeys` lo usa.
- Errores de usuario → `InvalidOperationException` con mensaje en español.

- [ ] Pruebas: `Export_RoundTrip`, `Export_RejectsNoKeys`, `Load_AcceptsRawLst`, `Load_FindsNestedLst`, `Load_RejectsZipWithoutKeys`, `Load_RejectsEmptyOrTooBig`, `Load_RejectsCorruptZip`, `Apply_ReplacesTargetKeys`, `Apply_RejectsMissingTargetFolder`, `SuggestedFileName_Sanitizes` (`"a/b:c"` → sin `/` ni `:`, termina en `.zip`).
- [ ] `.\build.ps1 -Test` → falla.
- [ ] Implementar.
- [ ] `.\build.ps1 -Test` → `ALL PASSED`.

### Task 2: Interfaz

**Files:** Modify `src/App/MainWindow.cs`, `src/App/ConfirmDialog.cs`, `src/App/Theme.cs` (estilo CheckBox oscuro); Create `src/App/Settings.cs` (`static string LastFolder {get;set;}`).

- [ ] Botones exportar (⤓, deshabilitado sin teclas) e importar (⤒, deshabilitado con Steam/Dota abiertos) en cada tarjeta.
- [ ] `ConfirmDialog.Ask(..., string checkText, out bool isChecked)`; casilla en copiar e importar si el destino tiene teclas.
- [ ] `.\build.ps1` sin errores; abrir la app y capturar; comprobar que las teclas reales no cambian (hash).
