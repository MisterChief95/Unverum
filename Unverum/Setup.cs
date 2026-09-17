using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;

namespace Unverum;

public static class Setup
{
    public static string GetMD5Checksum(string filename)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filename);
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "");
    }
    public static bool CheckPatch(string exe)
    {
        // Remove old costume patch for DBFZ
        if (Global.CurrentGame == "Dragon Ball FighterZ")
        {
            var checksum = GetMD5Checksum(exe).ToLowerInvariant();
            var originalExe = exe.Replace("-eac-nop-loaded", string.Empty);
            // Check if old patch exe is still there
            switch (checksum)
            {
                // Old patched exes
                case "2f9c8178fc8a5cdb3ac1ff8fee888f89":
                case "ae356567060af7ffa7d0d9e293fafbd9":
                case "8c442c2913b2be5e4a21730c7c15b39f":
                case "d21dbaa08aba836a799254b4125967a9":
                case "ca33c47b6df1f561a068c627786226df":
                    File.Copy(originalExe, exe, true);
                    break;
                default:
                    if (!File.Exists(exe))
                        File.Copy(originalExe, exe, true);
                    break;
            }
        }
        var PatchPath = $"{Path.GetDirectoryName(Global.CurrentGameConfig.Launcher)}" +
            $"{Global.s}RED{Global.s}Binaries{Global.s}Win64";
        if (Global.CurrentGame == "Dragon Ball FighterZ")
            PatchPath = $"{Path.GetDirectoryName(Global.CurrentGameConfig.Launcher)}";
        else if (Global.CurrentGame == "Scarlet Nexus")
            PatchPath = $"{Path.GetDirectoryName(Global.CurrentGameConfig.Launcher)}" +
            $"{Global.s}ScarletNexus{Global.s}Binaries{Global.s}Win64";
        else if (Global.CurrentGame == "Dragon Ball Sparking! ZERO")
            PatchPath = $"{Path.GetDirectoryName(Global.CurrentGameConfig.Launcher)}" +
            $"{Global.s}SparkingZERO{Global.s}Binaries{Global.s}Win64";
        // Check if files exist
        switch (Global.CurrentGame)
        {
            case "Dragon Ball FighterZ":
                if (!File.Exists($"{PatchPath}{Global.s}plugins{Global.s}DBFZExtraCostumesPatch.asi")
                    || GetMD5Checksum($"{PatchPath}{Global.s}plugins{Global.s}DBFZExtraCostumesPatch.asi").Equals("e99c11be64f7fb81e8b2eebdff72b164", StringComparison.InvariantCultureIgnoreCase)
                    || GetMD5Checksum($"{PatchPath}{Global.s}plugins{Global.s}DBFZExtraCostumesPatch.asi").Equals("960d0f042522485a56665e156c0d9820", StringComparison.InvariantCultureIgnoreCase))
                    GetPatchFiles(PatchPath);
                break;
            case "Scarlet Nexus":
                if (!File.Exists($"{PatchPath}{Global.s}plugins{Global.s}ScarletNexusUTOCSigBypass.asi"))
                    GetPatchFiles(PatchPath);
                break;
            case "Dragon Ball Sparking! ZERO":
                if (!File.Exists($"{PatchPath}{Global.s}plugins{Global.s}DBSparkingZeroUTOCBypass.asi"))
                    GetPatchFiles(PatchPath);
                break;
        }
        return true;
    }
    public static void GetPatchFiles(string PatchPath)
    {
        foreach (var file in Assembly.GetExecutingAssembly().GetManifestResourceNames()
                .Where(x => x.Contains($"{Global.CurrentGame.Replace(" ", "_").Replace("-", "_").Replace("!", "_")}.Patch")))
        {
            using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(file)
                ?? throw new InvalidOperationException($"Embedded patch resource {file} is missing.");
            var split = file.Split('.');
            var index = split.ToList().FindIndex(x => x == "Patch");
            var path = $"{PatchPath}{Global.s}{string.Join(Global.s, split[(index + 1)..(split.Length - 1)])}.{split[split.Length - 1]}";
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? PatchPath);
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            resource.CopyTo(stream);
        }
    }
    public static bool Generic(string exe, string projectName, string defaultPath, string? otherExe = null, string? steamId = null, bool epic = false)
    {
        // Get install path from registry
        if (steamId != null && !epic)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {steamId}");
                if (key?.GetValue("InstallLocation") is string installLocation)
                    defaultPath = $"{installLocation}{Global.s}{exe}";
            }
            catch (Exception)
            {
            }
        }
        if (!File.Exists(defaultPath))
        {
            if (!epic)
                Global.logger.WriteLine("Couldn't find install path in registry, select path to exe instead", LoggerType.Warning);
            OpenFileDialog dialog = new()
            {
                DefaultExt = ".exe",
                Filter = otherExe == null ? $"Executable Files ({exe})|{exe}"
                    : $"Executable Files ({exe};{otherExe})|{exe};{otherExe}",
                Title = otherExe == null ? $"Select {exe} from your Steam Install folder"
                    : $"Select {exe} from your Steam Install folder or {otherExe} from your Epic Games install folder",
                Multiselect = false,
                InitialDirectory = Global.assemblyLocation
            };
            dialog.ShowDialog();
            if (!string.IsNullOrEmpty(dialog.FileName)
                && (Path.GetFileName(dialog.FileName).Equals(exe, StringComparison.InvariantCultureIgnoreCase)
                || (otherExe != null && Path.GetFileName(dialog.FileName).Equals(otherExe, StringComparison.InvariantCultureIgnoreCase))))
                defaultPath = dialog.FileName;
            else if (!string.IsNullOrEmpty(dialog.FileName))
            {
                Global.logger.WriteLine("Invalid .exe chosen", LoggerType.Error);
                return false;
            }
            else
                return false;
        }
        var parent = Path.GetDirectoryName(defaultPath);
        var ModsFolder = $"{parent}{Global.s}{projectName}{Global.s}Content{Global.s}Paks{Global.s}~mods";
        Directory.CreateDirectory(ModsFolder);
        Global.CurrentGameConfig.ModsFolder = ModsFolder;
        Global.CurrentGameConfig.Launcher = defaultPath;

        if (Global.CurrentGame == "Dragon Ball Sparking! ZERO"
            || Global.CurrentGame == "Scarlet Nexus")
            CheckPatch(Global.CurrentGameConfig.Launcher);

        Global.UpdateConfig();
        Global.logger.WriteLine($"Setup completed for {Global.CurrentGame}!", LoggerType.Info);
        return true;
    }
    public static bool GBVSR()
    {
        var defaultPath = @"C:\Program Files (x86)\Steam\steamapps\common\Granblue Fantasy Versus Rising\GBVSR.exe";
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2157560");
            if (key?.GetValue("InstallLocation") is string installLocation)
                defaultPath = $"{installLocation}{Global.s}GBVSR.exe";
        }
        catch (Exception)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2667960");
                if (key?.GetValue("InstallLocation") is string installLocation)
                    defaultPath = $"{installLocation}{Global.s}GBVSR.exe";
            }
            catch (Exception)
            {

            }
        }
        if (!File.Exists(defaultPath))
        {
            Global.logger.WriteLine("Couldn't find install path in registry, select path to exe instead", LoggerType.Warning);
            OpenFileDialog dialog = new()
            {
                DefaultExt = ".exe",
                Filter = "Executable Files (GBVSR.exe)|GBVSR.exe",
                Title = "Select GBVSR.exe from your Steam Install folder",
                Multiselect = false,
                InitialDirectory = Global.assemblyLocation
            };
            dialog.ShowDialog();
            if (!string.IsNullOrEmpty(dialog.FileName)
                && Path.GetFileName(dialog.FileName).Equals("GBVSR.exe", StringComparison.InvariantCultureIgnoreCase))
                defaultPath = dialog.FileName;
            else if (!string.IsNullOrEmpty(dialog.FileName))
            {
                Global.logger.WriteLine("Invalid .exe chosen", LoggerType.Error);
                return false;
            }
            else
                return false;
        }
        var parent = Path.GetDirectoryName(defaultPath);
        var ModsFolder = $"{parent}{Global.s}RED{Global.s}Content{Global.s}Paks{Global.s}~mods";
        Directory.CreateDirectory(ModsFolder);
        Global.CurrentGameConfig.ModsFolder = ModsFolder;
        Global.CurrentGameConfig.Launcher = defaultPath;
        Global.UpdateConfig();
        Global.logger.WriteLine($"Setup completed for {Global.CurrentGame}!", LoggerType.Info);
        return true;
    }

    public static bool Win64FolderSetup(string exe, string projectName, string gameName, string steamId)
    {
        var defaultPath = $@"C:\Program Files (x86)\Steam\steamapps\common\{gameName}\{projectName}\Binaries\Win64\{exe}";
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {steamId}");
            if (key?.GetValue("InstallLocation") is string installLocation)
                defaultPath = $"{installLocation}{Global.s}{projectName}{Global.s}Binaries{Global.s}Win64{Global.s}{exe}";
        }
        catch (Exception)
        {
        }
        if (!File.Exists(defaultPath))
        {
            Global.logger.WriteLine("Couldn't find install path in registry, select path to exe instead", LoggerType.Warning);
            OpenFileDialog dialog = new()
            {
                DefaultExt = ".exe",
                Filter = $"Executable Files ({exe})|{exe}",
                Title = $"Select {exe} from your Steam Install folder",
                Multiselect = false,
                InitialDirectory = Global.assemblyLocation
            };
            dialog.ShowDialog();
            if (!string.IsNullOrEmpty(dialog.FileName)
                && Path.GetFileName(dialog.FileName).Equals(exe, StringComparison.InvariantCultureIgnoreCase))
                defaultPath = dialog.FileName;
            else if (!string.IsNullOrEmpty(dialog.FileName))
            {
                Global.logger.WriteLine("Invalid .exe chosen", LoggerType.Error);
                return false;
            }
            else
                return false;
        }
        var parent = defaultPath.Replace($"{Global.s}{projectName}{Global.s}Binaries{Global.s}Win64{Global.s}{exe}", string.Empty);
        var paks = $"{parent}{Global.s}{projectName}{Global.s}Content{Global.s}Paks";
        var ModsFolder = $"{paks}{Global.s}~mods";
        Directory.CreateDirectory(ModsFolder);
        Global.CurrentGameConfig.ModsFolder = ModsFolder;
        Global.CurrentGameConfig.Launcher = defaultPath;
        Global.UpdateConfig();
        Global.logger.WriteLine($"Setup completed for {Global.CurrentGame}!", LoggerType.Info);
        return true;
    }
    public static bool KHIII()
    {
        OpenFileDialog dialog = new()
        {
            DefaultExt = ".exe",
            Filter = "Executable Files (KINGDOM HEARTS III.exe)|KINGDOM HEARTS III.exe",
            Title = "Select KINGDOM HEARTS III.exe from your Epic Games Install folder",
            Multiselect = false,
            InitialDirectory = Global.assemblyLocation
        };
        dialog.ShowDialog();
        if (!string.IsNullOrEmpty(dialog.FileName)
            && Path.GetFileName(dialog.FileName).Equals("KINGDOM HEARTS III.exe", StringComparison.InvariantCultureIgnoreCase))
        {
            var parent = dialog.FileName.Replace($"{Global.s}Binaries{Global.s}Win64{Global.s}KINGDOM HEARTS III.exe", string.Empty, StringComparison.InvariantCultureIgnoreCase);
            var paks = $"{parent}{Global.s}Content{Global.s}Paks";
            var ModsFolder = $"{paks}{Global.s}~mods";
            Directory.CreateDirectory(ModsFolder);
            Global.CurrentGameConfig.ModsFolder = ModsFolder;
            Global.CurrentGameConfig.Launcher = dialog.FileName;
            Global.UpdateConfig();
            Global.logger.WriteLine($"Setup completed for {Global.CurrentGame}!", LoggerType.Info);
            return true;
        }
        else if (!string.IsNullOrEmpty(dialog.FileName))
            Global.logger.WriteLine("Invalid .exe chosen", LoggerType.Error);
        return false;
    }
    public static void CheckJFPatch(string exe)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("Unverum.Resources.JUMP_FORCE.exe")
            ?? throw new InvalidOperationException("Embedded JUMP_FORCE.exe resource is missing.");
        if (stream.Length != new FileInfo(exe).Length)
        {
            byte[] buffer = new byte[stream.Length];
            stream.ReadExactly(buffer);
            File.WriteAllBytes(exe, buffer);
            Global.logger.WriteLine($"{exe} patched to ignore EasyAntiCheat.", LoggerType.Info);
        }
        else
            Global.logger.WriteLine($"{exe} already patched to ignore EasyAntiCheat.", LoggerType.Info);
    }
    public static bool JF()
    {
        var defaultPath = @"C:\Program Files (x86)\Steam\steamapps\common\JUMP FORCE\JUMP_FORCE.exe";
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 816020");
            if (key?.GetValue("InstallLocation") is string installLocation)
                defaultPath = $"{installLocation}{Global.s}JUMP_FORCE.exe";
        }
        catch (Exception)
        {
        }
        if (!File.Exists(defaultPath))
        {
            Global.logger.WriteLine("Couldn't find install path in registry, select path to exe instead", LoggerType.Warning);
            OpenFileDialog dialog = new()
            {
                DefaultExt = ".exe",
                Filter = "Executable Files (JUMP_FORCE.exe)|JUMP_FORCE.exe",
                Title = "Select JUMP_FORCE.exe from your Steam Install folder",
                Multiselect = false,
                InitialDirectory = Global.assemblyLocation
            };
            dialog.ShowDialog();
            if (!string.IsNullOrEmpty(dialog.FileName)
                && Path.GetFileName(dialog.FileName).Equals("JUMP_FORCE.exe", StringComparison.InvariantCultureIgnoreCase))
                defaultPath = dialog.FileName;
            else if (!string.IsNullOrEmpty(dialog.FileName))
            {
                Global.logger.WriteLine("Invalid .exe chosen", LoggerType.Error);
                return false;
            }
            else
                return false;
        }
        var parent = Path.GetDirectoryName(defaultPath);
        var ModsFolder = $"{parent}{Global.s}JUMP_FORCE{Global.s}Content{Global.s}Paks{Global.s}~mods";

        CheckJFPatch(defaultPath);

        Directory.CreateDirectory(ModsFolder);
        Global.CurrentGameConfig.ModsFolder = ModsFolder;
        Global.CurrentGameConfig.Launcher = defaultPath;
        Global.UpdateConfig();
        Global.logger.WriteLine("Setup completed!", LoggerType.Info);
        return true;
    }
    public static bool DBFZ()
    {
        var defaultPath = @"C:\Program Files (x86)\Steam\steamapps\common\DRAGON BALL FighterZ\DBFighterZ.exe";
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 678950");
            if (key?.GetValue("InstallLocation") is string installLocation)
                defaultPath = $"{installLocation}{Global.s}DBFighterZ.exe";
        }
        catch (Exception)
        {
        }
        if (!File.Exists(defaultPath))
        {
            Global.logger.WriteLine("Couldn't find install path in registry, select path to exe instead", LoggerType.Warning);
            OpenFileDialog dialog = new()
            {
                DefaultExt = ".exe",
                Filter = "Executable Files (DBFighterZ.exe)|DBFighterZ.exe",
                Title = "Select DBFighterZ.exe from your Steam Install folder",
                Multiselect = false,
                InitialDirectory = Global.assemblyLocation
            };
            dialog.ShowDialog();
            if (!string.IsNullOrEmpty(dialog.FileName)
                && Path.GetFileName(dialog.FileName).Equals("DBFighterZ.exe", StringComparison.InvariantCultureIgnoreCase))
                defaultPath = dialog.FileName;
            else if (!string.IsNullOrEmpty(dialog.FileName))
            {
                Global.logger.WriteLine("Invalid .exe chosen", LoggerType.Error);
                return false;
            }
            else
                return false;
        }
        var parent = Path.GetDirectoryName(defaultPath);
        var launcher = $"{parent}{Global.s}RED{Global.s}Binaries{Global.s}Win64{Global.s}RED-Win64-Shipping.exe";
        var renamedLauncher = $"{parent}{Global.s}RED{Global.s}Binaries{Global.s}Win64{Global.s}RED-Win64-Shipping-eac-nop-loaded.exe";
        var ModsFolder = $"{parent}{Global.s}RED{Global.s}Content{Global.s}Paks{Global.s}~mods";
        if (File.Exists(launcher))
            File.Copy(launcher, renamedLauncher, true);
        if (!File.Exists(renamedLauncher))
        {
            Global.logger.WriteLine($"Couldn't find {renamedLauncher}, select the correct exe", LoggerType.Error);
            return false;
        }


        Directory.CreateDirectory(ModsFolder);
        Global.CurrentGameConfig.ModsFolder = ModsFolder;
        Global.CurrentGameConfig.Launcher = renamedLauncher;

        CheckPatch(renamedLauncher);

        Global.UpdateConfig();
        Global.logger.WriteLine("Setup completed!", LoggerType.Info);
        return true;

    }

    // TODO: disable Launch/Build if current launcher option setup isn't complete
    public static bool SMTV(bool emu)
    {
        if (emu)
        {
            // Select emulator path
            OpenFileDialog dialog = new()
            {
                DefaultExt = ".exe",
                Filter = "Emulator Exe|yuzu.exe; Ryujinx.exe",
                Title = "Select Executable for Emulator (yuzu.exe or Ryujinx.exe)",
                Multiselect = false,
                InitialDirectory = Global.assemblyLocation
            };
            dialog.ShowDialog();
            if (!string.IsNullOrEmpty(dialog.FileName)
                    && (Path.GetFileName(dialog.FileName).Equals("yuzu.exe", StringComparison.InvariantCultureIgnoreCase)
                    || Path.GetFileName(dialog.FileName).Equals("Ryujinx.exe", StringComparison.InvariantCultureIgnoreCase)))
                Global.CurrentGameConfig.Launcher = dialog.FileName;
            else if (!string.IsNullOrEmpty(dialog.FileName))
            {
                Global.logger.WriteLine("Invalid .exe chosen", LoggerType.Error);
                return false;
            }
            else
                return false;

            // Select game path
            dialog.FileName = string.Empty;
            dialog.DefaultExt = ".xci;.nsp";
            dialog.Filter = "Switch Game|*.xci;*.nsp";
            dialog.Title = "Select Switch Game for Emulator to Launch";
            dialog.Multiselect = false;
            dialog.InitialDirectory = Global.assemblyLocation;
            dialog.ShowDialog();
            if (string.IsNullOrEmpty(dialog.FileName))
                return false;
            Global.CurrentGameConfig.GamePath = dialog.FileName;
        }

        var openFolder = new CommonOpenFileDialog
        {
            AllowNonFileSystemItems = true,
            IsFolderPicker = true,
            EnsurePathExists = true,
            EnsureValidNames = true,
            Multiselect = false,
            Title = "Select Mod Folder (NA: 010063B012DC6000, EU: 0100B870126CE000, JP: 01006BD0095F4000, HK/TW: 010038D0133C2000, KR: 0100FB70133C0000)"
        };
        var selected = true;
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (openFolder.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var hash = Path.GetFileName(openFolder.FileName ?? string.Empty).ToLowerInvariant();
                switch (hash)
                {
                    case "010063b012dc6000":
                    case "0100b870126ce000":
                    case "01006bd0095f4000":
                    case "010038d0133c2000":
                    case "0100fb70133c0000":
                        Global.CurrentGameConfig.ModsFolder = $"{openFolder.FileName}{Global.s}Unverum Mods{Global.s}romfs{Global.s}Project{Global.s}Content{Global.s}Paks{Global.s}~mods";
                        Global.CurrentGameConfig.PatchesFolder = $"{openFolder.FileName}{Global.s}Unverum Mods{Global.s}exefs";
                        break;
                    default:
                        Global.logger.WriteLine("Invalid output path chosen", LoggerType.Error);
                        selected = false;
                        break;
                }
            }
            else
                selected = false;
        });
        Global.UpdateConfig();
        if (selected)
            Global.logger.WriteLine("Setup completed!", LoggerType.Info);
        return selected;
    }
}
