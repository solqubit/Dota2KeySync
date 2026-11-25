using System;
using System.IO;

namespace Dota2KeySync.App
{
    /// <summary>Tiny per-user settings file: remembers the last folder used to export/import keys.</summary>
    public static class Settings
    {
        static string FilePath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                    "Dota2KeySync", "settings.txt");
            }
        }

        public static string LastFolder
        {
            get
            {
                try
                {
                    if (File.Exists(FilePath))
                    {
                        string dir = File.ReadAllText(FilePath).Trim();
                        if (dir.Length > 0 && Directory.Exists(dir)) return dir;
                    }
                }
                catch
                {
                    // fall through to the default
                }
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            set
            {
                try
                {
                    if (string.IsNullOrEmpty(value)) return;
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    File.WriteAllText(FilePath, value);
                }
                catch
                {
                    // not worth bothering the user about
                }
            }
        }
    }
}
