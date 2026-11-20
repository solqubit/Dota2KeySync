# Dota2KeySync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Un `.exe` de Windows que lista cuentas de Steam, copia las teclas de Dota 2 entre cuentas (reemplazo total) y elimina la carpeta de cuentas olvidadas.

**Architecture:** Lógica pura en `src/Core` (parser VDF, escaneo de cuentas, operaciones de archivos) probada con un ejecutable de pruebas de consola contra una carpeta Steam falsa. UI WPF en `src/App`, sin compilación XAML (UI construida en código o con `XamlReader.Parse`).

**Tech Stack:** C# 5, .NET Framework 4.8, WPF, `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.

**Spec:** `docs/superpowers/specs/2026-11-18-dota2keysync-design.md`

## Global Constraints
- C# 5 solamente: nada de `$""`, `?.`, `nameof`, miembros `=>`, ni `using static`.
- Sin copias de seguridad ni `.bak` creados por la app; sin red.
- Sin commits con atribución; no se sube nada a GitHub (no hay commits en este plan).
- `AccountID = SteamID64 - 76561197960265728`; ignorar `userdata\0`.
- Teclas: `userdata\<id>\570\remote\cfg\dotakeys_personal.lst`.
- Textos de la UI en español.

## Review Focus
- `loginusers.vdf` ausente o vacío → todas las carpetas salen como "Olvidada", sin crash.
- Carpeta de `userdata` no numérica (p.ej. `anonymous`) → se ignora.
- Destino sin carpeta `570\remote\cfg` → se crea y la copia funciona.
- Origen == destino o origen sin teclas → la operación se rechaza con mensaje.
- Borrado con archivos de solo lectura dentro → se quitan atributos y se borra igual.

---

### Task 1: Parser VDF + escaneo de cuentas

**Files:**
- Create: `src/Core/Vdf.cs`, `src/Core/SteamAccounts.cs`, `tests/CoreTests.cs`, `build.ps1`

**Interfaces:**
- Produces:
  - `static class Vdf { static Dictionary<string, object> Parse(string text); }` — valores son `string` o `Dictionary<string, object>`; claves case-insensitive; soporta `\"` y `\\` escapados y comentarios `//`.
  - `class SteamAccount { string AccountId; string SteamId64; string PersonaName; string AccountName; bool Remembered; bool MostRecent; bool HasDotaKeys; string FolderPath; string KeysPath; string AvatarPath /* null si no existe */; }`
  - `static class SteamLocator { static string FindSteamPath(); }` — registro `HKCU\Software\Valve\Steam\SteamPath`, normaliza `/` a `\`; null si no hay.
  - `static class AccountScanner { static List<SteamAccount> Scan(string steamPath); }` — orden: recordadas primero (MostRecent arriba), luego olvidadas; nombre de olvidadas desde `localconfig.vdf` (primer `"PersonaName"` por regex, sin parsear todo); si no hay nombre → `"Cuenta " + AccountId`.
- `build.ps1 [-Test]`: compila `bin\Dota2KeySync.exe` (target winexe) y `bin\CoreTests.exe`; con `-Test` ejecuta las pruebas; código de salida != 0 si algo falla.

- [ ] **Step 1: Pruebas** en `tests/CoreTests.cs` (mini runner propio: `Check(bool, string)`, cuenta fallos, `return fails`). Crean Steam falso en `%TEMP%\d2ks_test_<guid>`:
  - `Vdf_ParsesNested`: `"users"{"7656"{"AccountName" "a\"b"}}` → `users["7656"]["accountname"] == "a\"b"`.
  - `Scan_RememberedAndForgotten`: loginusers con SteamID64 `76561198000000001` (Persona `Jugador`, MostRecent `1`); carpetas `39734273` (con teclas), `5555` (localconfig con `"PersonaName" "Viejo"`, sin teclas), `0`, `anonymous` → 2 cuentas; `[0]` AccountId `39734273`, Remembered, HasDotaKeys; `[1]` `5555`, !Remembered, PersonaName `Viejo`, SteamId64 `76561197960271283`.
  - `Scan_NoLoginUsers`: sin `loginusers.vdf` → cuentas todas !Remembered, sin excepción.
- [ ] **Step 2:** `.\build.ps1 -Test` → falla (no compila).
- [ ] **Step 3:** Implementar `Vdf`, `SteamAccount`, `SteamLocator`, `AccountScanner`.
- [ ] **Step 4:** `.\build.ps1 -Test` → `ALL PASSED`.

### Task 2: Operaciones (copiar, eliminar, procesos)

**Files:**
- Create: `src/Core/KeyOps.cs`; Modify: `tests/CoreTests.cs`

**Interfaces:**
- Consumes: `SteamAccount` (Task 1).
- Produces:
  - `static class KeyOps { static void CopyKeys(SteamAccount from, SteamAccount to); static void DeleteAccountFolder(SteamAccount acc); static List<string> RunningBlockers(); }`
  - `CopyKeys` lanza `InvalidOperationException` con mensaje en español si `from == to` (mismo AccountId) o `!from.HasDotaKeys`; crea directorios; `File.Copy(..., overwrite:true)` tras quitar ReadOnly del destino.
  - `DeleteAccountFolder` lanza `InvalidOperationException` si `acc.Remembered`; quita atributos ReadOnly recursivamente y `Directory.Delete(path, true)`.
  - `RunningBlockers` devuelve `"Steam"`/`"Dota 2"` según `Process.GetProcessesByName("steam" | "dota2")`.

- [ ] **Step 1: Pruebas**: `Copy_ReplacesContent` (destino con contenido distinto queda byte-idéntico al origen, y no aparece ningún archivo nuevo en la carpeta cfg salvo el `.lst`); `Copy_CreatesMissingCfg`; `Copy_RejectsSameOrEmpty`; `Delete_RemovesForgottenWithReadOnly`; `Delete_RejectsRemembered`.
- [ ] **Step 2:** `.\build.ps1 -Test` → falla.
- [ ] **Step 3:** Implementar `KeyOps`.
- [ ] **Step 4:** `.\build.ps1 -Test` → `ALL PASSED`.

### Task 3: Interfaz WPF

**Files:**
- Create: `src/App/App.cs` (`[STAThread] Main`), `src/App/MainWindow.cs`, `src/App/Theme.cs` (colores/estilos), `src/App/app.manifest` opcional (DPI aware).

**Interfaces:**
- Consumes: `SteamLocator`, `AccountScanner`, `KeyOps`.

Requisitos (del spec): ventana oscura sin borde del sistema (`WindowStyle=None`, barra propia arrastrable con minimizar/cerrar), acento rojo Dota `#E5452F`, fondo `#14161B`, tarjetas `#1E2129`. Tarjeta por cuenta: avatar circular (PNG o inicial), nombre, `@usuario` y AccountID, etiqueta "Recordada"(verde)/"Olvidada"(ámbar), indicador "Teclas ✓"/"Sin teclas", papelera solo en olvidadas. Panel inferior: `Origen ▼ → Destino ▼ [Copiar teclas]`. Barra de estado + botón "Actualizar". Antes de copiar/borrar: si `RunningBlockers()` no vacío → aviso "Cierra Steam/Dota 2 antes de continuar" y no se hace nada. Confirmaciones con diálogo propio oscuro: copiar ("Las teclas de X reemplazarán por completo las de Y") y borrar ("Se eliminará toda la carpeta de X. No se puede deshacer"). Sin Steam encontrado → mensaje en la ventana.

- [ ] **Step 1:** Implementar; `.\build.ps1` compila sin errores ni warnings de C# 5.
- [ ] **Step 2:** Abrir `bin\Dota2KeySync.exe`, captura de pantalla: se ven las cuentas reales del PC como Recordadas con teclas; con Steam abierto, "Copiar teclas" muestra el aviso y no modifica archivos (verificar hash de los `.lst` antes/después).
