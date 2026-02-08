using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mdk.Hub.Features.Storage;

/// <summary>
///     Provides abstracted filesystem operations for both production and test environments.
///     Production: Uses real filesystem with standard Windows/Linux paths.
///     Test: Uses in-memory storage or isolated temporary directories.
/// </summary>
public interface IFileStorageService
{
    // ========================================
    // PATH RESOLUTION
    // ========================================
    
    /// <summary>
    ///     Gets path for roaming application data.
    ///     Production: %AppData%/MDK2 or ~/.config/MDK2
    ///     Test: In-memory or temp directory
    /// </summary>
    string GetApplicationDataPath(params string[] subPaths);
    
    /// <summary>
    ///     Gets path for local (non-roaming) application data.
    ///     Production: %LocalAppData%/MDK2 or ~/.local/share/MDK2
    ///     Test: In-memory or temp directory
    /// </summary>
    string GetLocalApplicationDataPath(params string[] subPaths);
    
    /// <summary>
    ///     Gets path for temporary files.
    ///     Production: %Temp% or /tmp
    ///     Test: Isolated temp directory
    /// </summary>
    string GetTempPath(params string[] subPaths);
    
    /// <summary>
    ///     Gets the user's documents folder.
    ///     Production: %MyDocuments% or ~/Documents
    ///     Test: Mock documents directory
    /// </summary>
    string GetDocumentsPath(params string[] subPaths);
    
    /// <summary>
    ///     Gets the Space Engineers data path.
    ///     Production: %AppData%/SpaceEngineers
    ///     Test: Mock SE directory
    /// </summary>
    string GetSpaceEngineersDataPath(params string[] subPaths);
    
    // ========================================
    // FILE OPERATIONS - Read
    // ========================================
    
    /// <summary>
    ///     Checks if a file exists.
    /// </summary>
    bool FileExists(string path);
    
    /// <summary>
    ///     Reads all text from a file.
    /// </summary>
    string ReadAllText(string path);
    
    /// <summary>
    ///     Reads all text from a file asynchronously.
    /// </summary>
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Reads all bytes from a file.
    /// </summary>
    byte[] ReadAllBytes(string path);
    
    /// <summary>
    ///     Reads all bytes from a file asynchronously.
    /// </summary>
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Opens a file for reading.
    /// </summary>
    Stream OpenRead(string path);
    
    // ========================================
    // FILE OPERATIONS - Write
    // ========================================
    
    /// <summary>
    ///     Writes text to a file, creating or overwriting.
    /// </summary>
    void WriteAllText(string path, string contents);
    
    /// <summary>
    ///     Writes text to a file asynchronously, creating or overwriting.
    /// </summary>
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Appends text to a file.
    /// </summary>
    void AppendAllText(string path, string contents);
    
    /// <summary>
    ///     Appends text to a file asynchronously.
    /// </summary>
    Task AppendAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Writes bytes to a file, creating or overwriting.
    /// </summary>
    void WriteAllBytes(string path, byte[] bytes);
    
    /// <summary>
    ///     Writes bytes to a file asynchronously, creating or overwriting.
    /// </summary>
    Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default);
    
    // ========================================
    // FILE OPERATIONS - Management
    // ========================================
    
    /// <summary>
    ///     Deletes a file.
    /// </summary>
    void DeleteFile(string path);
    
    /// <summary>
    ///     Copies a file.
    /// </summary>
    void CopyFile(string sourcePath, string destinationPath, bool overwrite = false);
    
    /// <summary>
    ///     Moves/renames a file.
    /// </summary>
    void MoveFile(string sourcePath, string destinationPath);
    
    /// <summary>
    ///     Gets the last write time of a file in UTC.
    /// </summary>
    DateTime GetLastWriteTimeUtc(string path);
    
    // ========================================
    // DIRECTORY OPERATIONS
    // ========================================
    
    /// <summary>
    ///     Checks if a directory exists.
    /// </summary>
    bool DirectoryExists(string path);
    
    /// <summary>
    ///     Creates a directory and all parent directories.
    /// </summary>
    void CreateDirectory(string path);
    
    /// <summary>
    ///     Deletes a directory.
    /// </summary>
    void DeleteDirectory(string path, bool recursive = false);
    
    /// <summary>
    ///     Gets all files in a directory matching a pattern.
    /// </summary>
    string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    
    /// <summary>
    ///     Gets all subdirectories in a directory.
    /// </summary>
    string[] GetDirectories(string path);
    
    /// <summary>
    ///     Enumerates files in a directory (lazy evaluation).
    /// </summary>
    IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
}
