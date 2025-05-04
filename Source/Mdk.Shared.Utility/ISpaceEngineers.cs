namespace Mdk2.Shared.Utility
{
    public interface ISpaceEngineers
    {
        /// <summary>
        /// Attempts to get the install path of Space Engineers.
        /// </summary>
        /// <param name="subfolders">The desired subfolder path, if any</param>
        /// <returns></returns>
        string GetInstallPath(params string[] subfolders);

        /// <summary>
        /// Attempts to get the default data path for Space Engineers.
        /// </summary>
        /// <param name="subfolders">The desired subfolder path, if any</param>
        /// <returns></returns>
        string GetDataPath(params string[] subfolders);
    }
}