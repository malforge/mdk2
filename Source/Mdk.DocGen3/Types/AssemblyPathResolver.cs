using System.Runtime.InteropServices;

namespace Mdk.DocGen3.Types;

public static class AssemblyPathResolver
{
    [DllImport("fusion.dll")]
    private static extern int CreateAssemblyCache(out IAssemblyCache ppAsmCache, uint reserved);

    /// <summary>
    ///     Resolves the full file path of an assembly by:
    ///     1) custom directories (if any),
    ///     2) GAC,
    ///     3) runtime directory.
    /// </summary>
    /// <param name="fullAssemblyName">
    ///     e.g. "System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
    /// </param>
    /// <param name="customFolders">
    ///     optional list of absolute folder paths to search first
    /// </param>
    /// <returns>absolute path to the DLL</returns>
    public static string Resolve(string fullAssemblyName, IEnumerable<string>? customFolders = null)
    {
        var nameOnly = fullAssemblyName.Split(',')[0] + ".dll";

        // 1) custom folders
        if (customFolders != null)
        {
            foreach (var folder in customFolders)
            {
                if (string.IsNullOrWhiteSpace(folder)) continue;
                var candidate = Path.Combine(folder, nameOnly);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        // 2) GAC
        var info = new AssemblyInfo
        {
            cchBuf = 1024,
            currentAssemblyPath = new string('\0', 1024),
            cbAssemblyInfo = (uint)Marshal.SizeOf<AssemblyInfo>()
        };

        CreateAssemblyCache(out var cache, 0);
        if (cache.QueryAssemblyInfo(0, fullAssemblyName, ref info) == 0)
            return info.currentAssemblyPath.TrimEnd('\0');

        // 3) runtime dir
        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        var runtimeCandidate = Path.Combine(runtimeDir, nameOnly);
        if (File.Exists(runtimeCandidate))
            return runtimeCandidate;

        throw new FileNotFoundException($"Could not locate '{fullAssemblyName}' in custom folders, GAC, or '{runtimeDir}'.");
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
    private interface IAssemblyCache
    {
        int QueryAssemblyInfo(uint flags, [MarshalAs(UnmanagedType.LPWStr)] string assemblyName, ref AssemblyInfo assemblyInfo);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AssemblyInfo
    {
        public uint cbAssemblyInfo;
        public uint assemblyFlags;
        public ulong assemblySize;
        [MarshalAs(UnmanagedType.LPWStr)] public string currentAssemblyPath;
        public uint cchBuf;
    }
}