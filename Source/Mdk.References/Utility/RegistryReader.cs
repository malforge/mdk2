// Mdk.References
// 
// Copyright 2023 Morten A. Lyrstad

using System;
using System.Runtime.InteropServices;

namespace Mdk2.References.Utility
{
    public static class RegistryReader
    {
        static readonly IntPtr HkeyCurrentUser = (IntPtr)0x80000001;

        public static string GetSteamPath() => GetValue(@"Software\Valve\Steam", "SteamPath");

        public static string GetSteamExe() => GetValue(@"Software\Valve\Steam", "SteamExe");

        [DllImport("advapi32.dll", EntryPoint = "RegOpenKeyEx", SetLastError = true)]
        static extern int RegOpenKeyEx(IntPtr hKey, string lpSubKey, int ulOptions, int samDesired, out IntPtr phkResult);

        [DllImport("advapi32.dll", EntryPoint = "RegQueryValueEx")]
        static extern int RegQueryValueEx(IntPtr hKey, string lpValueName, int lpReserved, out uint lpType, IntPtr lpData, ref uint lpcbData);

        static string GetValue(string subKey, string valueName)
        {
            var openResult = RegOpenKeyEx(HkeyCurrentUser, subKey, 0, 0x20019, out var keyHandle);
            if (openResult != 0)
                throw new InvalidOperationException("Failed to open registry key.");

            uint dataLen = 1024;
            var pData = Marshal.AllocHGlobal((int)dataLen);
            var queryResult = RegQueryValueEx(keyHandle, valueName, 0, out _, pData, ref dataLen);

            if (queryResult != 0)
            {
                Marshal.FreeHGlobal(pData);
                throw new InvalidOperationException("Failed to query registry value.");
            }

            var value = Marshal.PtrToStringAnsi(pData);
            Marshal.FreeHGlobal(pData);

            return value;
        }
    }
}