using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Dota2KeySync.Core
{
    public class SteamAccount
    {
        public string AccountId { get; set; }
        public string SteamId64 { get; set; }
        public string PersonaName { get; set; }
        public string AccountName { get; set; }
        public bool Remembered { get; set; }
        public bool MostRecent { get; set; }
        /// <summary>True when loginusers.vdf exists but could not be read, so Remembered is not trustworthy.</summary>
        public bool RememberedUnknown { get; set; }
        public bool HasDotaKeys { get; set; }
        public string FolderPath { get; set; }
        public string KeysPath { get; set; }
        /// <summary>Path to the cached avatar PNG, or null when Steam has none.</summary>
        public string AvatarPath { get; set; }

        public string DisplayName
        {
            get { return string.IsNullOrEmpty(PersonaName) ? "Cuenta " + AccountId : PersonaName; }
        }
    }

    public static class SteamLocator
    {
        public static string FindSteamPath()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key == null) return null;
                    var path = key.GetValue("SteamPath") as string;
                    if (string.IsNullOrEmpty(path)) return null;
                    path = path.Replace('/', '\\');
                    return Directory.Exists(path) ? path : null;
                }
            }
            catch
            {
                return null;
            }
        }
    }

    public static class AccountScanner
    {
        public const long SteamId64Base = 76561197960265728L;
        public const string DotaAppId = "570";
        public const string KeysFileName = "dotakeys_personal.lst";

        public static string KeysPathFor(string folderPath)
        {
            return Path.Combine(folderPath, DotaAppId, "remote", "cfg", KeysFileName);
        }

        public static List<SteamAccount> Scan(string steamPath)
        {
            var remembered = ReadLoginUsers(steamPath);
            var result = new List<SteamAccount>();
            string userdata = Path.Combine(steamPath, "userdata");
            if (!Directory.Exists(userdata)) return result;

            foreach (var dir in Directory.GetDirectories(userdata))
            {
                string name = Path.GetFileName(dir);
                long accountId;
                if (!TryParseAccountFolder(name, out accountId)) continue;

                string id64 = (accountId + SteamId64Base).ToString();
                var acc = new SteamAccount
                {
                    AccountId = name,
                    SteamId64 = id64,
                    FolderPath = dir,
                    KeysPath = KeysPathFor(dir),
                };
                acc.HasDotaKeys = File.Exists(acc.KeysPath);

                Dictionary<string, object> info;
                if (remembered == null)
                {
                    acc.RememberedUnknown = true;
                }
                else if (remembered.TryGetValue(id64, out info))
                {
                    acc.Remembered = true;
                    acc.AccountName = Get(info, "AccountName");
                    acc.PersonaName = Get(info, "PersonaName");
                    acc.MostRecent = Get(info, "MostRecent") == "1";
                }
                if (string.IsNullOrEmpty(acc.PersonaName))
                    acc.PersonaName = ReadLocalPersona(dir);
                if (string.IsNullOrEmpty(acc.PersonaName))
                    acc.PersonaName = "Cuenta " + name;

                string avatar = Path.Combine(steamPath, "config", "avatarcache", id64 + ".png");
                acc.AvatarPath = File.Exists(avatar) ? avatar : null;

                result.Add(acc);
            }

            return result
                .OrderByDescending(a => a.Remembered)
                .ThenByDescending(a => a.MostRecent)
                .ThenBy(a => a.PersonaName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

/// <summary>Accepts only canonical positive decimal folder names ("123", not "0123" or "+5").</summary>
        public static bool TryParseAccountFolder(string name, out long accountId)
        {
            return long.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out accountId)
                && accountId > 0
                && accountId.ToString(CultureInfo.InvariantCulture) == name;
        }

        /// <summary>
        /// Fresh check against loginusers.vdf: true = remembered, false = not listed (or file missing),
        /// null = file exists but can't be read/understood right now.
        /// </summary>
        public static bool? IsRemembered(string steamPath, string steamId64)
        {
            var map = ReadLoginUsers(steamPath);
            if (map == null) return null;
            return map.ContainsKey(steamId64);
        }

        /// <summary>Returns an empty map when the file is missing, and null when it exists but is unreadable.</summary>
        static Dictionary<string, Dictionary<string, object>> ReadLoginUsers(string steamPath)
        {
            var map = new Dictionary<string, Dictionary<string, object>>();
            string file = Path.Combine(steamPath, "config", "loginusers.vdf");
            if (!File.Exists(file)) return map;
            try
            {
                var root = Vdf.Parse(File.ReadAllText(file));
                object usersObj;
                if (!root.TryGetValue("users", out usersObj)) return null;
                var users = usersObj as Dictionary<string, object>;
                if (users == null) return null;
                foreach (var kv in users)
                {
                    var info = kv.Value as Dictionary<string, object>;
                    if (info != null) map[kv.Key] = info;
                }
                return map;
            }
            catch
            {
                // Locked or corrupt: we can't tell who is remembered.
                return null;
            }
        }

        static readonly Regex PersonaRegex = new Regex("\"PersonaName\"\\s+\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.IgnoreCase);

        static string ReadLocalPersona(string folder)
        {
            try
            {
                string file = Path.Combine(folder, "config", "localconfig.vdf");
                if (!File.Exists(file)) return null;
                var m = PersonaRegex.Match(File.ReadAllText(file));
                return m.Success ? m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\") : null;
            }
            catch
            {
                return null;
            }
        }

        static string Get(Dictionary<string, object> d, string key)
        {
            object v;
            return d.TryGetValue(key, out v) ? v as string : null;
        }
    }
}
