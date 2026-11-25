using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dota2KeySync.Core;

namespace Dota2KeySync.Tests
{
    static class CoreTests
    {
        static int fails = 0;
        static int checks = 0;

        static void Check(bool ok, string what)
        {
            checks++;
            if (!ok) { fails++; Console.WriteLine("FAIL: " + what); }
        }

        static void Run(string name, Action test)
        {
            try { test(); Console.WriteLine("ok   " + name); }
            catch (Exception ex) { fails++; Console.WriteLine("FAIL " + name + ": " + ex.GetType().Name + " " + ex.Message); }
        }

        static int Main()
        {
            Run("Vdf_ParsesNested", Vdf_ParsesNested);
            Run("Scan_RememberedAndForgotten", Scan_RememberedAndForgotten);
            Run("Scan_NoLoginUsers", Scan_NoLoginUsers);
            Run("Copy_ReplacesContent", Copy_ReplacesContent);
            Run("Copy_CreatesMissingCfg", Copy_CreatesMissingCfg);
            Run("Copy_RejectsSameOrEmpty", Copy_RejectsSameOrEmpty);
            Run("Delete_RemovesForgottenWithReadOnly", Delete_RemovesForgottenWithReadOnly);
            Run("Delete_RejectsRemembered", Delete_RejectsRemembered);
            Run("Scan_LockedLoginUsers_MarksUnknown", Scan_LockedLoginUsers_MarksUnknown);
            Run("Delete_RejectsNowRemembered", Delete_RejectsNowRemembered);
            Run("Delete_DoesNotFollowJunction", Delete_DoesNotFollowJunction);
            Run("Copy_SetsFreshTimestampAndWritable", Copy_SetsFreshTimestampAndWritable);
            Run("Copy_RejectsMissingTargetFolder", Copy_RejectsMissingTargetFolder);
            Run("Scan_IgnoresNonCanonicalNames", Scan_IgnoresNonCanonicalNames);
            Run("Delete_RejectsPathOutsideUserdata", Delete_RejectsPathOutsideUserdata);
            Run("Export_RoundTrip", Export_RoundTrip);
            Run("Export_RejectsNoKeys", Export_RejectsNoKeys);
            Run("Load_AcceptsRawLst", Load_AcceptsRawLst);
            Run("Load_FindsNestedLst", Load_FindsNestedLst);
            Run("Load_RejectsZipWithoutKeys", Load_RejectsZipWithoutKeys);
            Run("Load_RejectsEmptyOrTooBig", Load_RejectsEmptyOrTooBig);
            Run("Load_RejectsCorruptZip", Load_RejectsCorruptZip);
            Run("Apply_ReplacesTargetKeys", Apply_ReplacesTargetKeys);
            Run("Apply_RejectsMissingTargetFolder", Apply_RejectsMissingTargetFolder);
            Run("SuggestedFileName_Sanitizes", SuggestedFileName_Sanitizes);
            Run("Export_RefusesInsideSteam", Export_RefusesInsideSteam);
            Run("Load_DetectsZipWithoutExtension", Load_DetectsZipWithoutExtension);
            Run("Load_RejectsNonKeysFiles", Load_RejectsNonKeysFiles);
            Console.WriteLine(fails == 0 ? "ALL PASSED (" + checks + " checks)" : fails + " FAILED");
            return fails;
        }

        // ---------- helpers ----------

        static string NewSteam()
        {
            string root = Path.Combine(Path.GetTempPath(), "d2ks_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "config"));
            Directory.CreateDirectory(Path.Combine(root, "userdata"));
            return root;
        }

        static void WriteLoginUsers(string root, string body)
        {
            File.WriteAllText(Path.Combine(root, "config", "loginusers.vdf"), "\"users\"\n{\n" + body + "}\n");
        }

        static string UserBlock(string id64, string account, string persona, bool mostRecent)
        {
            return "\t\"" + id64 + "\"\n\t{\n\t\t\"AccountName\"\t\t\"" + account + "\"\n\t\t\"PersonaName\"\t\t\"" + persona +
                   "\"\n\t\t\"RememberPassword\"\t\t\"1\"\n\t\t\"MostRecent\"\t\t\"" + (mostRecent ? "1" : "0") + "\"\n\t}\n";
        }

        static string AddUser(string root, string accountId, string keysContent, string localPersona)
        {
            string dir = Path.Combine(root, "userdata", accountId);
            Directory.CreateDirectory(Path.Combine(dir, "config"));
            if (localPersona != null)
                File.WriteAllText(Path.Combine(dir, "config", "localconfig.vdf"),
                    "\"UserLocalConfigStore\"\n{\n\t\"friends\"\n\t{\n\t\t\"PersonaName\"\t\t\"" + localPersona + "\"\n\t}\n}\n");
            if (keysContent != null)
            {
                string cfg = Path.Combine(dir, "570", "remote", "cfg");
                Directory.CreateDirectory(cfg);
                File.WriteAllText(Path.Combine(cfg, "dotakeys_personal.lst"), keysContent);
            }
            return dir;
        }

        static SteamAccount Find(List<SteamAccount> list, string id)
        {
            return list.First(a => a.AccountId == id);
        }

        // ---------- Task 1 ----------

        static void Vdf_ParsesNested()
        {
            var d = Vdf.Parse("// comment\n\"users\"\n{\n\t\"7656\"\n\t{\n\t\t\"AccountName\"\t\t\"a\\\"b\"\n\t\t\"Path\"\t\"c:\\\\x\"\n\t}\n}\n");
            var users = (Dictionary<string, object>)d["users"];
            var u = (Dictionary<string, object>)users["7656"];
            Check((string)u["accountname"] == "a\"b", "escaped quote + case-insensitive key");
            Check((string)u["path"] == "c:\\x", "escaped backslash");
        }

        static void Scan_RememberedAndForgotten()
        {
            string root = NewSteam();
            WriteLoginUsers(root, UserBlock("76561198000000001", "jugador1", "Jugador", true));
            AddUser(root, "39734273", "KEYS", null);
            AddUser(root, "5555", null, "Viejo");
            Directory.CreateDirectory(Path.Combine(root, "userdata", "0"));
            Directory.CreateDirectory(Path.Combine(root, "userdata", "anonymous"));

            var list = AccountScanner.Scan(root);
            Check(list.Count == 2, "2 accounts, got " + list.Count);
            var a = list[0];
            Check(a.AccountId == "39734273", "first is remembered");
            Check(a.Remembered && a.HasDotaKeys && a.MostRecent, "remembered flags");
            Check(a.PersonaName == "Jugador" && a.AccountName == "jugador1", "names from loginusers");
            Check(a.SteamId64 == "76561198000000001", "steamid64");
            var b = list[1];
            Check(b.AccountId == "5555" && !b.Remembered && !b.HasDotaKeys, "forgotten flags");
            Check(b.PersonaName == "Viejo", "forgotten persona from localconfig");
            Check(b.SteamId64 == "76561197960271283", "forgotten steamid64 computed");
            Check(b.KeysPath == Path.Combine(root, "userdata", "5555", "570", "remote", "cfg", "dotakeys_personal.lst"), "keys path");
            Directory.Delete(root, true);
        }

        static void Scan_NoLoginUsers()
        {
            string root = NewSteam();
            AddUser(root, "123", "K", null);
            var list = AccountScanner.Scan(root);
            Check(list.Count == 1 && !list[0].Remembered, "all forgotten without loginusers");
            Check(list[0].PersonaName == "Cuenta 123", "fallback name");
            Directory.Delete(root, true);
        }

        // ---------- Task 2 ----------

        static void Copy_ReplacesContent()
        {
            string root = NewSteam();
            AddUser(root, "1", "SOURCE KEYS\nline2", null);
            string dst = AddUser(root, "2", "OLD", null);
            var list = AccountScanner.Scan(root);
            KeyOps.CopyKeys(Find(list, "1"), Find(list, "2"));
            string cfg = Path.Combine(dst, "570", "remote", "cfg");
            Check(File.ReadAllText(Path.Combine(cfg, "dotakeys_personal.lst")) == "SOURCE KEYS\nline2", "content replaced");
            Check(Directory.GetFiles(cfg).Length == 1, "no extra files (no backups)");
            Directory.Delete(root, true);
        }

        static void Copy_CreatesMissingCfg()
        {
            string root = NewSteam();
            AddUser(root, "1", "K1", null);
            string dst = AddUser(root, "2", null, null);
            var list = AccountScanner.Scan(root);
            KeyOps.CopyKeys(Find(list, "1"), Find(list, "2"));
            Check(File.ReadAllText(Path.Combine(dst, "570", "remote", "cfg", "dotakeys_personal.lst")) == "K1", "created cfg");
            Directory.Delete(root, true);
        }

        static void Copy_RejectsSameOrEmpty()
        {
            string root = NewSteam();
            AddUser(root, "1", "K1", null);
            AddUser(root, "2", null, null);
            var list = AccountScanner.Scan(root);
            bool threwSame = false, threwEmpty = false;
            try { KeyOps.CopyKeys(Find(list, "1"), Find(list, "1")); } catch (InvalidOperationException) { threwSame = true; }
            try { KeyOps.CopyKeys(Find(list, "2"), Find(list, "1")); } catch (InvalidOperationException) { threwEmpty = true; }
            Check(threwSame, "same account rejected");
            Check(threwEmpty, "source without keys rejected");
            Check(File.ReadAllText(Find(list, "1").KeysPath) == "K1", "untouched");
            Directory.Delete(root, true);
        }

        static void Delete_RemovesForgottenWithReadOnly()
        {
            string root = NewSteam();
            string dir = AddUser(root, "9", "K", "Old");
            string ro = Path.Combine(dir, "570", "remote", "cfg", "dotakeys_personal.lst");
            File.SetAttributes(ro, FileAttributes.ReadOnly);
            var list = AccountScanner.Scan(root);
            KeyOps.DeleteAccountFolder(Find(list, "9"));
            Check(!Directory.Exists(dir), "folder deleted");
            Directory.Delete(root, true);
        }

        static void Delete_RejectsRemembered()
        {
            string root = NewSteam();
            WriteLoginUsers(root, UserBlock("76561197960265729", "acc", "Me", true));
            string dir = AddUser(root, "1", "K", null);
            var list = AccountScanner.Scan(root);
            bool threw = false;
            try { KeyOps.DeleteAccountFolder(Find(list, "1")); } catch (InvalidOperationException) { threw = true; }
            Check(threw && Directory.Exists(dir), "remembered not deleted");
            Directory.Delete(root, true);
        }

        // ---------- review fixes ----------

        static void Scan_LockedLoginUsers_MarksUnknown()
        {
            string root = NewSteam();
            WriteLoginUsers(root, UserBlock("76561197960265729", "acc", "Me", true));
            string dir = AddUser(root, "1", "K", null);
            List<SteamAccount> list;
            using (new FileStream(Path.Combine(root, "config", "loginusers.vdf"), FileMode.Open, FileAccess.Read, FileShare.None))
            {
                list = AccountScanner.Scan(root);
            }
            Check(list[0].RememberedUnknown, "unreadable loginusers -> RememberedUnknown");
            bool threw = false;
            try { KeyOps.DeleteAccountFolder(list[0]); } catch (InvalidOperationException) { threw = true; }
            Check(threw && Directory.Exists(dir), "unknown state not deletable");
            Directory.Delete(root, true);
        }

        static void Delete_RejectsNowRemembered()
        {
            string root = NewSteam();
            string dir = AddUser(root, "1", "K", null);
            var list = AccountScanner.Scan(root);
            Check(!list[0].Remembered, "starts forgotten");
            WriteLoginUsers(root, UserBlock("76561197960265729", "acc", "Me", true)); // user logs in later
            bool threw = false;
            try { KeyOps.DeleteAccountFolder(list[0]); } catch (InvalidOperationException) { threw = true; }
            Check(threw && Directory.Exists(dir), "stale forgotten but now remembered is not deleted");
            Directory.Delete(root, true);
        }

        static void Delete_DoesNotFollowJunction()
        {
            string root = NewSteam();
            string outside = Path.Combine(root, "outside");
            Directory.CreateDirectory(outside);
            string precious = Path.Combine(outside, "precious.txt");
            File.WriteAllText(precious, "keep");
            File.SetAttributes(precious, FileAttributes.ReadOnly | FileAttributes.Hidden);
            string dir = AddUser(root, "9", "K", null);
            var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c mklink /J \"" + Path.Combine(dir, "link") + "\" \"" + outside + "\"")
            { CreateNoWindow = true, UseShellExecute = false };
            System.Diagnostics.Process.Start(psi).WaitForExit();
            Check(Directory.Exists(Path.Combine(dir, "link")), "junction created");

            var list = AccountScanner.Scan(root);
            KeyOps.DeleteAccountFolder(Find(list, "9"));
            Check(!Directory.Exists(dir), "account folder deleted");
            Check(File.Exists(precious), "junction target kept");
            Check((File.GetAttributes(precious) & FileAttributes.ReadOnly) != 0, "junction target attributes untouched");
            File.SetAttributes(precious, FileAttributes.Normal);
            Directory.Delete(root, true);
        }

        static void Copy_SetsFreshTimestampAndWritable()
        {
            string root = NewSteam();
            string src = AddUser(root, "1", "K1", null);
            AddUser(root, "2", "OLD", null);
            string srcKeys = AccountScanner.KeysPathFor(src);
            File.SetLastWriteTimeUtc(srcKeys, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            File.SetAttributes(srcKeys, FileAttributes.ReadOnly);
            var list = AccountScanner.Scan(root);
            KeyOps.CopyKeys(Find(list, "1"), Find(list, "2"));
            string dst = Find(list, "2").KeysPath;
            Check((File.GetAttributes(dst) & FileAttributes.ReadOnly) == 0, "target writable");
            Check(File.GetLastWriteTimeUtc(dst) > DateTime.UtcNow.AddMinutes(-1), "target timestamp is fresh");
            File.SetAttributes(srcKeys, FileAttributes.Normal);
            Directory.Delete(root, true);
        }

        static void Copy_RejectsMissingTargetFolder()
        {
            string root = NewSteam();
            AddUser(root, "1", "K1", null);
            string dst = AddUser(root, "2", null, null);
            var list = AccountScanner.Scan(root);
            Directory.Delete(dst, true); // deleted externally after scan
            bool threw = false;
            try { KeyOps.CopyKeys(Find(list, "1"), Find(list, "2")); } catch (InvalidOperationException) { threw = true; }
            Check(threw && !Directory.Exists(dst), "does not recreate deleted account");
            Directory.Delete(root, true);
        }

        static void Scan_IgnoresNonCanonicalNames()
        {
            string root = NewSteam();
            AddUser(root, "123", "K", null);
            Directory.CreateDirectory(Path.Combine(root, "userdata", "0123"));
            Directory.CreateDirectory(Path.Combine(root, "userdata", "+5"));
            var list = AccountScanner.Scan(root);
            Check(list.Count == 1 && list[0].AccountId == "123", "only canonical numeric names, got " + list.Count);
            Directory.Delete(root, true);
        }

        static void Delete_RejectsPathOutsideUserdata()
        {
            string root = NewSteam();
            string victim = Path.Combine(root, "config", "77");
            Directory.CreateDirectory(victim);
            var fake = new SteamAccount { AccountId = "77", SteamId64 = "76561197960265805", FolderPath = victim, KeysPath = AccountScanner.KeysPathFor(victim) };
            bool threw = false;
            try { KeyOps.DeleteAccountFolder(fake); } catch (InvalidOperationException) { threw = true; }
            Check(threw && Directory.Exists(victim), "path outside userdata refused");
            Directory.Delete(root, true);
        }

        // ---------- export / import ----------

        static bool Throws(Action a)
        {
            try { a(); return false; } catch (InvalidOperationException) { return true; }
        }

        static void MakeZip(string path, string entryName, byte[] data)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var zip = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create))
            {
                var e = zip.CreateEntry(entryName);
                using (var s = e.Open()) s.Write(data, 0, data.Length);
            }
        }

        /// <summary>A realistic keybind file body (real ones start with "KeyBindings").</summary>
        static string K(string body)
        {
            return "\"KeyBindings\"\n{\n\t\"" + body + "\"\n}\n";
        }

        /// <summary>A folder outside the fake Steam root, for exported zips.</summary>
        static string OutDir()
        {
            string d = Path.Combine(Path.GetTempPath(), "d2ks_out_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(d);
            return d;
        }

        static void Export_RoundTrip()
        {
            string root = NewSteam();
            string outDir = OutDir();
            WriteLoginUsers(root, UserBlock("76561198000000001", "jugador1", "Jugador", true));
            AddUser(root, "39734273", K("bind x"), null);
            var list = AccountScanner.Scan(root);
            string zip = Path.Combine(outDir, "backup.zip");
            KeyArchive.Export(list[0], zip);
            var pkg = KeyArchive.Load(zip);
            Check(System.Text.Encoding.UTF8.GetString(pkg.Keys) == K("bind x"), "keys round-trip");
            Check(pkg.SourceName == "Jugador", "source name, got " + pkg.SourceName);
            Check(pkg.SourceAccountId == "39734273", "source account id");
            Check(!string.IsNullOrEmpty(pkg.Date), "date present");
            Check(pkg.FilePath == zip, "file path");

            // exporting again over the same file replaces it and leaves no temp files
            KeyArchive.Export(list[0], zip);
            Check(Directory.GetFiles(outDir).Length == 1, "overwrite leaves exactly one file");
            Directory.Delete(root, true);
            Directory.Delete(outDir, true);
        }

        static void Export_RefusesInsideSteam()
        {
            string root = NewSteam();
            AddUser(root, "1", K("a"), null);
            var list = AccountScanner.Scan(root);
            string inside = Path.Combine(root, "userdata", "1", "570", "remote", "cfg", "backup.zip");
            Check(Throws(() => KeyArchive.Export(list[0], inside)) && !File.Exists(inside), "export into Steam refused");
            string over = list[0].KeysPath;
            Check(Throws(() => KeyArchive.Export(list[0], over)) && File.ReadAllText(over) == K("a"), "export over keys file refused");
            Check(KeyArchive.IsInsideSteam(list[0], inside) && !KeyArchive.IsInsideSteam(list[0], Path.GetTempPath()), "IsInsideSteam");
            Directory.Delete(root, true);
        }

        static void Load_DetectsZipWithoutExtension()
        {
            string dir = OutDir();
            string f = Path.Combine(dir, "teclas.zip.download");
            MakeZip(f, "dotakeys_personal.lst", System.Text.Encoding.UTF8.GetBytes(K("Z")));
            Check(System.Text.Encoding.UTF8.GetString(KeyArchive.Load(f).Keys) == K("Z"), "zip detected by signature");
            Directory.Delete(dir, true);
        }

        static void Load_RejectsNonKeysFiles()
        {
            string dir = OutDir();
            string txt = Path.Combine(dir, "notas.txt");
            File.WriteAllText(txt, "esto es una nota cualquiera");
            Check(Throws(() => KeyArchive.Load(txt)), "random text rejected");
            string bin = Path.Combine(dir, "foto.lst");
            File.WriteAllBytes(bin, new byte[] { 0x22, 0x4B, 0x00, 0x01, 0x02 });
            Check(Throws(() => KeyArchive.Load(bin)), "binary rejected");
            Directory.Delete(dir, true);
        }

        static void Export_RejectsNoKeys()
        {
            string root = NewSteam();
            AddUser(root, "5", null, null);
            var list = AccountScanner.Scan(root);
            string zip = Path.Combine(root, "x.zip");
            Check(Throws(() => KeyArchive.Export(list[0], zip)) && !File.Exists(zip), "no keys -> rejected, no file");
            Directory.Delete(root, true);
        }

        static void Load_AcceptsRawLst()
        {
            string root = NewSteam();
            string lst = Path.Combine(root, "keys.lst");
            File.WriteAllText(lst, K("RAW"));
            var pkg = KeyArchive.Load(lst);
            Check(System.Text.Encoding.UTF8.GetString(pkg.Keys) == K("RAW") && pkg.SourceName == null, "raw lst accepted");
            Directory.Delete(root, true);
        }

        static void Load_FindsNestedLst()
        {
            string root = NewSteam();
            string zip = Path.Combine(root, "n.zip");
            MakeZip(zip, "carpeta/DotaKeys_Personal.lst", System.Text.Encoding.UTF8.GetBytes(K("NESTED")));
            Check(System.Text.Encoding.UTF8.GetString(KeyArchive.Load(zip).Keys) == K("NESTED"), "nested, case-insensitive");
            Directory.Delete(root, true);
        }

        static void Load_RejectsZipWithoutKeys()
        {
            string root = NewSteam();
            string zip = Path.Combine(root, "w.zip");
            MakeZip(zip, "otra_cosa.txt", new byte[] { 1, 2, 3 });
            Check(Throws(() => KeyArchive.Load(zip)), "zip without keys rejected");
            Directory.Delete(root, true);
        }

        static void Load_RejectsEmptyOrTooBig()
        {
            string root = NewSteam();
            string empty = Path.Combine(root, "e.zip");
            MakeZip(empty, "dotakeys_personal.lst", new byte[0]);
            Check(Throws(() => KeyArchive.Load(empty)), "empty rejected");
            string big = Path.Combine(root, "b.zip");
            MakeZip(big, "dotakeys_personal.lst", new byte[KeyArchive.MaxKeysBytes + 1]);
            Check(Throws(() => KeyArchive.Load(big)), "too big rejected");
            string bigLst = Path.Combine(root, "b.lst");
            File.WriteAllBytes(bigLst, new byte[KeyArchive.MaxKeysBytes + 1]);
            Check(Throws(() => KeyArchive.Load(bigLst)), "too big raw rejected");
            Directory.Delete(root, true);
        }

        static void Load_RejectsCorruptZip()
        {
            string root = NewSteam();
            string zip = Path.Combine(root, "c.zip");
            File.WriteAllText(zip, "esto no es un zip");
            Check(Throws(() => KeyArchive.Load(zip)), "corrupt zip -> InvalidOperationException");
            Directory.Delete(root, true);
        }

        static void Apply_ReplacesTargetKeys()
        {
            string root = NewSteam();
            string outDir = OutDir();
            AddUser(root, "1", K("FROM"), null);
            AddUser(root, "2", "OLD", null);
            var list = AccountScanner.Scan(root);
            string zip = Path.Combine(outDir, "k.zip");
            KeyArchive.Export(Find(list, "1"), zip);
            var to = Find(list, "2");
            File.SetAttributes(to.KeysPath, FileAttributes.ReadOnly);
            KeyArchive.Apply(KeyArchive.Load(zip), to);
            Check(File.ReadAllText(to.KeysPath) == K("FROM"), "replaced");
            Check((File.GetAttributes(to.KeysPath) & FileAttributes.ReadOnly) == 0, "writable");
            Check(File.GetLastWriteTimeUtc(to.KeysPath) > DateTime.UtcNow.AddMinutes(-1), "fresh timestamp");
            Check(Directory.GetFiles(Path.GetDirectoryName(to.KeysPath)).Length == 1, "no extra files in Steam");
            Directory.Delete(root, true);
            Directory.Delete(outDir, true);
        }

        static void Apply_RejectsMissingTargetFolder()
        {
            string root = NewSteam();
            string lst = Path.Combine(root, "k.lst");
            File.WriteAllText(lst, K("K"));
            string dst = AddUser(root, "2", null, null);
            var list = AccountScanner.Scan(root);
            Directory.Delete(dst, true);
            Check(Throws(() => KeyArchive.Apply(KeyArchive.Load(lst), list[0])) && !Directory.Exists(dst), "missing target rejected");
            Directory.Delete(root, true);
        }

        static void SuggestedFileName_Sanitizes()
        {
            var acc = new SteamAccount { AccountId = "1", PersonaName = "a/b:c*?" };
            string name = KeyArchive.SuggestedFileName(acc, new DateTime(2026, 11, 24));
            Check(name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0, "no invalid chars: " + name);
            Check(name.EndsWith(".zip") && name.Contains("2026-11-24"), "zip with date: " + name);
        }
    }
}
