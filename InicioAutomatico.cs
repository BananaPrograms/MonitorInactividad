using System;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace MonitorInactividad
{
    public static class InicioAutomatico
    {
        public static void EnableAutoStart(string appName, string appPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EnableAutoStartWindows(appName, appPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                EnableAutoStartLinux(appName, appPath);
            }
        }

        public static void DisableAutoStart(string appName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DisableAutoStartWindows(appName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                DisableAutoStartLinux(appName);
            }
        }

        public static bool IsAutoStartEnabled(string appName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return IsAutoStartEnabledWindows(appName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return IsAutoStartEnabledLinux(appName);
            }

            return false;
        }

        [SupportedOSPlatform("windows")]
        private static void EnableAutoStartWindows(string appName, string appPath)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);

            if (key is null)
            {
                throw new Exception("No se pudo abrir la clave del registro.");
            }
            key.SetValue(appName, $"\"{appPath}\" --autostart");
        }

        [SupportedOSPlatform("windows")]
        private static void DisableAutoStartWindows(string appName)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (key is null)
            {
                throw new Exception("No se pudo abrir la clave del registro.");
            }
            key.DeleteValue(appName, false);
        }

        [SupportedOSPlatform("windows")]
        private static bool IsAutoStartEnabledWindows(string appName)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

            if (key is null)
                return false;

            return key.GetValue(appName) is not null;
        }

        private static void EnableAutoStartLinux(string appName, string appPath)
        {
            string autostartDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "autostart");

            Directory.CreateDirectory(autostartDir);

            string desktopFile = Path.Combine(autostartDir, $"{appName}.desktop");

            string content = $@"
                            [Desktop Entry]
                            Type=Application
                            Name={appName}
                            Exec={appPath} --autostart
                            X-GNOME-Autostart-enabled=true
                            ";

            File.WriteAllText(desktopFile, content);
        }

        private static void DisableAutoStartLinux(string appName)
        {
            string desktopFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config/autostart", $"{appName}.desktop");

            if (File.Exists(desktopFile))
                File.Delete(desktopFile);
        }

        private static bool IsAutoStartEnabledLinux(string appName)
        {
            string desktopFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "autostart",
                $"{appName}.desktop");

            return File.Exists(desktopFile);
        }

    }
}
