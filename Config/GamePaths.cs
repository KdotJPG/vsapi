﻿using System;
using System.IO;
using Vintagestory.API.Config;

namespace Vintagestory.API.Config
{
    public static class GamePaths
    {
        public static string AllowedNameChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890_-";

        public static string DataPath;
        public static string CustomLogPath;
        public static string AssetsPath { get; private set; }

        static GamePaths()
        {
            // Dev env
            if (RuntimeEnv.IsDevEnvironment)
            {
                DataPath = AppDomain.CurrentDomain.BaseDirectory;
            }
            else
            {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                DataPath = Path.Combine(appdata, "VintagestoryData");
            }

            // Prodution env
            if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets")))
            {
                AssetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
            }
            else

            // Denv env
            {
                AssetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "assets");
            }
        }

        public static string Binaries { get { return AppDomain.CurrentDomain.BaseDirectory; } }
        public static string BinariesMods { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods"); } }

        public static string Config { get { return DataPath; } }
        public static string ModConfig { get { return Path.Combine(DataPath, "ModConfig"); } }
        public static string Cache { get { return Path.Combine(DataPath, "Cache"); } }
        public static string Saves { get { return Path.Combine(DataPath, "Saves"); } }
        public static string OldSaves { get { return Path.Combine(DataPath, "OldSaves"); } }
        public static string BackupSaves { get { return Path.Combine(DataPath, "BackupSaves"); } }
        public static string PlayerData { get { return Path.Combine(DataPath, "Playerdata"); } }
        public static string Backups { get { return Path.Combine(DataPath, "Backups"); } }
        public static string Logs { get { return CustomLogPath != null ? CustomLogPath : Path.Combine(DataPath, "Logs"); } }
        public static string Macros { get { return Path.Combine(DataPath, "Macros"); } }
        public static string Screenshots { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Vintagestory"); } }
        public static string Videos { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Vintagestory"); } }
        public static string DataPathMods { get { return Path.Combine(DataPath, "Mods"); } }
        public static string DataPathServerMods { get { return Path.Combine(DataPath, "ModsByServer"); } }


        public static string DefaultSaveFilenameWithoutExtension = "default";
        

        public static void EnsurePathExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void EnsurePathsExist()
        {
            if (!Directory.Exists(Config))
            {
                Directory.CreateDirectory(Config);
            }

            if (!Directory.Exists(Cache))
            {
                Directory.CreateDirectory(Cache);
            }
            if (!Directory.Exists(Saves))
            {
                Directory.CreateDirectory(Saves);
            }
            if (!Directory.Exists(BackupSaves))
            {
                Directory.CreateDirectory(BackupSaves);
            }
            if (!Directory.Exists(PlayerData))
            {
                Directory.CreateDirectory(PlayerData);
            }
            if (!Directory.Exists(Backups))
            {
                Directory.CreateDirectory(Backups);
            }
            if (!Directory.Exists(Logs))
            {
                Directory.CreateDirectory(Logs);
            }
            if (!Directory.Exists(Macros))
            {
                Directory.CreateDirectory(Macros);
            }

            if (!Directory.Exists(DataPathMods))
            {
                Directory.CreateDirectory(DataPathMods);
            }

        }

        public static bool IsValidName(string s)
        {
            if (s.Length < 1 || s.Length > 128)
            {
                return false;
            }
            for (int i = 0; i < s.Length; i++)
            {
                if (!AllowedNameChars.Contains(s[i].ToString()))
                {
                    return false;
                }
            }
            return true;
        }


        public static string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

    }
}
