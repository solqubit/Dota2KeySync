# Dota2KeySync — Diseño

## Para qué
Pasar las teclas de Dota 2 de una cuenta de Steam a otra en el mismo PC, y limpiar
los datos de cuentas que ya se "olvidaron" en Steam (para no confundirse al buscar teclas).

## Objetivos
- Listar todas las cuentas que tienen carpeta en `Steam\userdata`, con nombre, avatar y estado.
- Copiar teclas de Dota de una cuenta origen a una cuenta destino: **reemplazo total**.
- Detectar cuentas olvidadas y eliminar **toda su carpeta** `userdata\<id>`.

## No-objetivos / anti-objetivos
- Sin copias de seguridad, sin "deshacer", sin archivos `.bak` creados por la app.
- No copia otros ajustes (`config.cfg`) — solo teclas.
- Nada de red, nada de GitHub, sin atribuciones.

## Datos de Steam
- Ruta de Steam: registro `HKCU\Software\Valve\Steam\SteamPath`.
- Cuentas recordadas: `config\loginusers.vdf` (SteamID64 → AccountName, PersonaName, MostRecent).
- Carpeta de cuenta: `userdata\<AccountID>` donde `AccountID = SteamID64 - 76561197960265728`.
- Nombre de cuenta olvidada: `userdata\<id>\config\localconfig.vdf` → primer `"PersonaName"`.
- Avatar: `config\avatarcache\<SteamID64>.png` (si existe; si no, inicial del nombre).
- Teclas: `userdata\<id>\570\remote\cfg\dotakeys_personal.lst`.
- Se ignora la carpeta `userdata\0`.

## Estados de cuenta
- **Recordada**: su SteamID64 está en `loginusers.vdf`.
- **Olvidada**: tiene carpeta en `userdata` pero no está en `loginusers.vdf`.
- Indicador "Tiene teclas de Dota" / "Sin teclas".

## Acciones
- **Copiar teclas**: elegir origen (debe tener teclas) y destino (recordada u olvidada, distinto del origen).
  Sobrescribe `dotakeys_personal.lst` del destino (crea `570\remote\cfg` si falta).
  Steam Cloud ve el archivo local cambiado y lo sube al abrir Dota con esa cuenta.
- **Eliminar cuenta olvidada**: borra `userdata\<id>` completo, tras confirmación en la app
  ("no se puede deshacer"). Solo disponible para cuentas olvidadas.
- Antes de cualquier acción, si `steam.exe` o `dota2.exe` están abiertos, la app avisa y bloquea
  la acción hasta que se cierren (botón "Reintentar"). Motivo: Steam Cloud podría devolver las teclas antiguas.

## Enfoques considerados
1. **C# WPF compilado con el `csc.exe` que trae Windows (.NET Framework 4.8)** — elegido.
   Un solo `.exe` pequeño, sin instalar nada, UI moderna con estilos propios.
2. Python + CustomTkinter + PyInstaller — `.exe` de ~15 MB y dependencias de pip.
3. Electron / web — demasiado pesado para algo tan simple.

## Interfaz
Ventana oscura, sin bordes del sistema (barra propia), acento rojo Dota.
- Lista de tarjetas de cuentas: avatar, nombre, usuario, AccountID, etiqueta Recordada/Olvidada, indicador de teclas.
- Panel inferior: `Origen [▼]  →  Destino [▼]   [Copiar teclas]`.
- Botón papelera en tarjetas de cuentas olvidadas.
- Barra de estado con avisos (Steam abierto, copia hecha, etc.) y botón "Actualizar".

## Construcción
- Código en `src/`, script `build.ps1` que compila a `Dota2KeySync.exe` con csc (C# 5, sin features modernas).
- Restricción: C# 5 → nada de `$""`, `?.`, ni miembros `=>`.
