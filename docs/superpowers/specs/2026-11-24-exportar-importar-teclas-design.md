# Exportar / importar teclas — Diseño (cambio pequeño)

## Para qué
Guardar, de forma opcional, las teclas de una cuenta antes de reemplazarlas, en un archivo
donde el usuario elija, y poder asignar después esas teclas a cualquier cuenta.

## Objetivos
- **Exportar** (botón por tarjeta, solo cuentas con teclas): "Guardar como" de Windows → `.zip`
  con `dotakeys_personal.lst` + `info.txt` (cuenta, AccountId, fecha). Nombre sugerido
  `Teclas Dota2 - <Nombre> - <yyyy-MM-dd>.zip`.
- **Importar** (botón por tarjeta): "Abrir" → `.zip` exportado o `.lst` suelto → confirmación con
  origen y fecha → reemplazo total de las teclas de esa cuenta.
- Casilla **desmarcada** en las confirmaciones de copiar e importar: "Exportar antes las teclas
  actuales de X" (solo si X tiene teclas). Si se marca y se cancela el guardado, no se hace nada.

## No-objetivos
- Nada se guarda dentro de la carpeta de Steam. La copia directa sigue siendo reemplazo total.
- Sin historial automático de reservas.

## Reglas
- Importar: bloqueado con Steam/Dota 2 abiertos, confirmación, re-chequeo tras confirmar. Exportar solo lee: permitido siempre.
- Rechazar: zip sin `dotakeys_personal.lst`, archivo vacío, > 1 MB, zip corrupto.
- Se recuerda la última carpeta usada (`%APPDATA%\Dota2KeySync\settings.txt`); por defecto Documentos.

## Enfoques
1. **Zip con teclas + ficha** — elegido: un solo archivo, dice de qué cuenta y fecha es.
2. `.lst` suelto — sin metadatos (se acepta al importar por comodidad).
3. Formato propio — sin ventaja frente al zip.
