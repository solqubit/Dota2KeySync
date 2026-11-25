using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Dota2KeySync.Core
{
    /// <summary>Keybinds loaded from an exported .zip or a raw .lst file.</summary>
    public class KeyPackage
    {
        public byte[] Keys { get; set; }
        /// <summary>Account name stored in the zip's info.txt; null for raw .lst files.</summary>
        public string SourceName { get; set; }
        public string SourceAccountId { get; set; }
        public string Date { get; set; }
        public string FilePath { get; set; }
    }

    /// <summary>Exports keybinds to a user-chosen .zip and imports them back into any account.</summary>
    public static class KeyArchive
    {
        public const long MaxKeysBytes = 1048576;
        const string InfoName = "info.txt";

        public static void Export(SteamAccount acc, string zipPath)
        {
            if (acc == null || !File.Exists(acc.KeysPath))
                throw new InvalidOperationException((acc == null ? "La cuenta" : acc.DisplayName) + " no tiene teclas de Dota 2 para exportar.");
            if (IsInsideSteam(acc, zipPath))
                throw new InvalidOperationException("Elige una carpeta fuera de Steam para guardar la exportación.");

            byte[] keys = File.ReadAllBytes(acc.KeysPath);
            string info =
                "Dota2KeySync=1\r\n" +
                "Cuenta=" + OneLine(acc.DisplayName) + "\r\n" +
                "AccountId=" + acc.AccountId + "\r\n" +
                "Fecha=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + "\r\n";

            // Write to a temp file first so a failure never leaves a half-written zip behind.
            string dir = Path.GetDirectoryName(Path.GetFullPath(zipPath));
            string tmp = Path.Combine(dir, Path.GetFileName(zipPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var fs = new FileStream(tmp, FileMode.CreateNew))
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    WriteEntry(zip, AccountScanner.KeysFileName, keys);
                    WriteEntry(zip, InfoName, Encoding.UTF8.GetBytes(info));
                }
                if (File.Exists(zipPath))
                {
                    // Replace swaps atomically, so the old zip is never lost if something fails.
                    File.SetAttributes(zipPath, FileAttributes.Normal);
                    File.Replace(tmp, zipPath, null);
                }
                else
                {
                    File.Move(tmp, zipPath);
                }
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        public static KeyPackage Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new InvalidOperationException("No se encontró el archivo.");

            var pkg = new KeyPackage { FilePath = path };
            // Decide by content, not extension: "x.zip.download" is still a zip, and a renamed file isn't.
            if (StartsWithZipSignature(path))
                LoadZip(path, pkg);
            else if (string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El archivo no es un zip válido.");
            else
                pkg.Keys = ReadLimited(path);

            if (pkg.Keys == null || pkg.Keys.Length == 0)
                throw new InvalidOperationException("El archivo de teclas está vacío.");
            if (!LooksLikeKeys(pkg.Keys))
                throw new InvalidOperationException("El archivo no parece un archivo de teclas de Dota 2.");
            return pkg;
        }

        static bool StartsWithZipSignature(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var head = new byte[4];
                int n = fs.Read(head, 0, 4);
                return n == 4 && head[0] == 0x50 && head[1] == 0x4B && head[2] == 0x03 && head[3] == 0x04;
            }
        }

        /// <summary>Dota 2 keybind files are text KeyValues starting with "KeyBindings".</summary>
        static bool LooksLikeKeys(byte[] data)
        {
            if (Array.IndexOf(data, (byte)0) >= 0) return false;
            string head = Encoding.UTF8.GetString(data, 0, Math.Min(data.Length, 4096));
            return head.IndexOf("KeyBindings", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static void Apply(KeyPackage pkg, SteamAccount to)
        {
            if (pkg == null || pkg.Keys == null || pkg.Keys.Length == 0)
                throw new InvalidOperationException("No hay teclas para importar.");
            if (to == null)
                throw new InvalidOperationException("Elige la cuenta que recibirá las teclas.");
            KeyOps.WriteKeys(to, pkg.Keys);
        }

        /// <summary>True when the path is inside the Steam folder that holds this account.</summary>
        public static bool IsInsideSteam(SteamAccount acc, string path)
        {
            if (acc == null || string.IsNullOrEmpty(acc.FolderPath) || string.IsNullOrEmpty(path)) return false;
            var userdata = new DirectoryInfo(acc.FolderPath).Parent;
            if (userdata == null || userdata.Parent == null) return false;
            string root = userdata.Parent.FullName.TrimEnd('\\') + "\\";
            string full = Path.GetFullPath(path).TrimEnd('\\') + "\\";
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        public static string SuggestedFileName(SteamAccount acc, DateTime now)
        {
            string name = acc == null ? "Cuenta" : acc.DisplayName;
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
            if (clean.Length == 0) clean = "Cuenta " + (acc == null ? "" : acc.AccountId);
            if (clean.Length > 60) clean = clean.Substring(0, 60);
            return "Teclas Dota2 - " + clean + " - " + now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".zip";
        }

        // ------------------------------------------------------------------

        static void LoadZip(string path, KeyPackage pkg)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    var keysEntry = zip.Entries.FirstOrDefault(e =>
                        string.Equals(e.Name, AccountScanner.KeysFileName, StringComparison.OrdinalIgnoreCase));
                    if (keysEntry == null)
                        throw new InvalidOperationException("El zip no contiene " + AccountScanner.KeysFileName + ".");
                    pkg.Keys = ReadEntry(keysEntry);

                    var infoEntry = zip.Entries.FirstOrDefault(e => string.Equals(e.Name, InfoName, StringComparison.OrdinalIgnoreCase));
                    if (infoEntry != null && infoEntry.Length < 64 * 1024)
                        ParseInfo(Encoding.UTF8.GetString(ReadEntry(infoEntry)), pkg);
                }
            }
            catch (InvalidOperationException) { throw; }
            catch (Exception ex)
            {
                if (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException)
                    throw new InvalidOperationException("No se pudo leer el zip: " + ex.Message);
                throw;
            }
        }

        static byte[] ReadEntry(ZipArchiveEntry entry)
        {
            if (entry.Length > MaxKeysBytes) throw TooBig();
            using (var s = entry.Open())
            using (var ms = new MemoryStream())
            {
                var buf = new byte[8192];
                int n;
                while ((n = s.Read(buf, 0, buf.Length)) > 0)
                {
                    ms.Write(buf, 0, n);
                    if (ms.Length > MaxKeysBytes) throw TooBig();
                }
                return ms.ToArray();
            }
        }

        static byte[] ReadLimited(string path)
        {
            if (new FileInfo(path).Length > MaxKeysBytes) throw TooBig();
            return File.ReadAllBytes(path);
        }

        static Exception TooBig()
        {
            return new InvalidOperationException("El archivo es demasiado grande para ser un archivo de teclas de Dota 2.");
        }

        static void ParseInfo(string text, KeyPackage pkg)
        {
            foreach (var raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                if (key.Equals("Cuenta", StringComparison.OrdinalIgnoreCase)) pkg.SourceName = val;
                else if (key.Equals("AccountId", StringComparison.OrdinalIgnoreCase)) pkg.SourceAccountId = val;
                else if (key.Equals("Fecha", StringComparison.OrdinalIgnoreCase)) pkg.Date = val;
            }
        }

        static void WriteEntry(ZipArchive zip, string name, byte[] data)
        {
            var e = zip.CreateEntry(name, CompressionLevel.Optimal);
            using (var s = e.Open()) s.Write(data, 0, data.Length);
        }

        static string OneLine(string s)
        {
            return (s ?? "").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
