using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Dota2KeySync.Core
{
    public static class KeyOps
    {
        /// <summary>Replaces the target's Dota 2 keybinds with the source's. No backup is kept.</summary>
        public static void CopyKeys(SteamAccount from, SteamAccount to)
        {
            if (from == null || to == null)
                throw new InvalidOperationException("Elige una cuenta de origen y una de destino.");
            if (from.AccountId == to.AccountId)
                throw new InvalidOperationException("El origen y el destino son la misma cuenta.");
            if (!File.Exists(from.KeysPath))
                throw new InvalidOperationException(from.DisplayName + " no tiene teclas de Dota 2 guardadas.");
            if (!Directory.Exists(to.FolderPath))
                throw new InvalidOperationException("La carpeta de " + to.DisplayName + " ya no existe. Pulsa actualizar.");

            Directory.CreateDirectory(Path.GetDirectoryName(to.KeysPath));
            if (File.Exists(to.KeysPath))
                File.SetAttributes(to.KeysPath, FileAttributes.Normal);
            File.Copy(from.KeysPath, to.KeysPath, true);

            // Writable for Dota, and newer than the cloud copy so Steam Cloud uploads it.
            File.SetAttributes(to.KeysPath, FileAttributes.Normal);
            File.SetLastWriteTimeUtc(to.KeysPath, DateTime.UtcNow);
            to.HasDotaKeys = true;
        }

        /// <summary>Permanently deletes a forgotten account's whole userdata folder.</summary>
        public static void DeleteAccountFolder(SteamAccount acc)
        {
            if (acc == null) throw new InvalidOperationException("No hay cuenta seleccionada.");

            // Path guard: must be <steam>\userdata\<canonical numeric id>.
            var dir = new DirectoryInfo(acc.FolderPath ?? "");
            long id;
            if (dir.Parent == null || dir.Parent.Parent == null
                || !string.Equals(dir.Parent.Name, "userdata", StringComparison.OrdinalIgnoreCase)
                || !AccountScanner.TryParseAccountFolder(dir.Name, out id)
                || dir.Name != acc.AccountId)
                throw new InvalidOperationException("Ruta de cuenta no válida: " + acc.FolderPath);

            // Fresh check: never trust a stale scan for something irreversible.
            bool? remembered = AccountScanner.IsRemembered(dir.Parent.Parent.FullName, (id + AccountScanner.SteamId64Base).ToString());
            if (acc.Remembered || acc.RememberedUnknown || remembered != false)
                throw new InvalidOperationException(remembered == null
                    ? "No se pudo leer la lista de cuentas de Steam. Inténtalo de nuevo con Steam cerrado."
                    : acc.DisplayName + " está recordada en Steam; solo se pueden eliminar cuentas olvidadas.");

            if (!dir.Exists) return;
            DeleteTree(dir);
        }

        /// <summary>Recursive delete that removes junctions/symlinks without entering them.</summary>
        static void DeleteTree(DirectoryInfo dir)
        {
            foreach (var sub in dir.GetDirectories())
            {
                if ((sub.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    sub.Attributes = FileAttributes.Directory | FileAttributes.ReparsePoint;
                    Directory.Delete(sub.FullName, false); // removes the link only
                }
                else
                {
                    DeleteTree(sub);
                }
            }
            foreach (var f in dir.GetFiles())
            {
                f.Attributes = FileAttributes.Normal;
                f.Delete();
            }
            dir.Attributes = FileAttributes.Directory;
            dir.Delete(false);
        }

        /// <summary>Names of running programs that must be closed before touching files.</summary>
        public static List<string> RunningBlockers()
        {
            var list = new List<string>();
            if (IsRunning("steam")) list.Add("Steam");
            if (IsRunning("dota2")) list.Add("Dota 2");
            return list;
        }

        static bool IsRunning(string name)
        {
            var procs = Process.GetProcessesByName(name);
            bool running = procs.Length > 0;
            foreach (var p in procs) p.Dispose();
            return running;
        }
    }
}
