using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mdk.Hub.Features.Storage;

/// <summary>
///     Fully in-memory filesystem implementation for unit tests. No disk I/O whatsoever.
/// </summary>
public class InMemoryFileStorageService : IFileStorageService
{
    readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, DateTime> _lastWriteTimes = new(StringComparer.OrdinalIgnoreCase);
    readonly string _appData = "/memory/appdata";
    readonly string _localAppData = "/memory/localappdata";
    readonly string _temp = "/memory/temp";
    readonly string _documents = "/memory/documents";
    readonly string _seData = "/memory/spaceengineers";
    
    public InMemoryFileStorageService()
    {
        // Create root directories
        _directories.Add(_appData);
        _directories.Add(_localAppData);
        _directories.Add(_temp);
        _directories.Add(_documents);
        _directories.Add(_seData);
    }
    
    // Path resolution
    public string GetApplicationDataPath(params string[] subPaths) 
        => NormalizePath(subPaths.Length == 0 ? _appData : Path.Combine([_appData, .. subPaths]));
    
    public string GetLocalApplicationDataPath(params string[] subPaths) 
        => NormalizePath(subPaths.Length == 0 ? _localAppData : Path.Combine([_localAppData, .. subPaths]));
    
    public string GetTempPath(params string[] subPaths) 
        => NormalizePath(subPaths.Length == 0 ? _temp : Path.Combine([_temp, .. subPaths]));
    
    public string GetDocumentsPath(params string[] subPaths) 
        => NormalizePath(subPaths.Length == 0 ? _documents : Path.Combine([_documents, .. subPaths]));
    
    public string GetSpaceEngineersDataPath(params string[] subPaths) 
        => NormalizePath(subPaths.Length == 0 ? _seData : Path.Combine([_seData, .. subPaths]));
    
    // File operations - read
    public bool FileExists(string path) => _files.ContainsKey(NormalizePath(path));
    
    public string ReadAllText(string path)
    {
        var normalizedPath = NormalizePath(path);
        if (!_files.TryGetValue(normalizedPath, out var bytes))
            throw new FileNotFoundException($"File not found: {path}", path);
        return Encoding.UTF8.GetString(bytes);
    }
    
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) 
        => Task.FromResult(ReadAllText(path));
    
    public byte[] ReadAllBytes(string path)
    {
        var normalizedPath = NormalizePath(path);
        if (!_files.TryGetValue(normalizedPath, out var bytes))
            throw new FileNotFoundException($"File not found: {path}", path);
        return bytes;
    }
    
    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default) 
        => Task.FromResult(ReadAllBytes(path));
    
    public Stream OpenRead(string path) 
        => new MemoryStream(ReadAllBytes(path), false);
    
    // File operations - write
    public void WriteAllText(string path, string contents)
    {
        var normalizedPath = NormalizePath(path);
        var directory = Path.GetDirectoryName(normalizedPath);
        if (!string.IsNullOrEmpty(directory))
            EnsureDirectoryExists(directory);
        _files[normalizedPath] = Encoding.UTF8.GetBytes(contents);
        _lastWriteTimes[normalizedPath] = DateTime.UtcNow;
    }
    
    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        WriteAllText(path, contents);
        return Task.CompletedTask;
    }
    
    public void AppendAllText(string path, string contents)
    {
        var normalizedPath = NormalizePath(path);
        var existing = FileExists(normalizedPath) ? ReadAllText(normalizedPath) : string.Empty;
        WriteAllText(normalizedPath, existing + contents);
    }
    
    public Task AppendAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        AppendAllText(path, contents);
        return Task.CompletedTask;
    }
    
    public void WriteAllBytes(string path, byte[] bytes)
    {
        var normalizedPath = NormalizePath(path);
        var directory = Path.GetDirectoryName(normalizedPath);
        if (!string.IsNullOrEmpty(directory))
            EnsureDirectoryExists(directory);
        _files[normalizedPath] = bytes;
        _lastWriteTimes[normalizedPath] = DateTime.UtcNow;
    }
    
    public Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
    {
        WriteAllBytes(path, bytes);
        return Task.CompletedTask;
    }
    
    // File operations - management
    public void DeleteFile(string path)
    {
        var normalizedPath = NormalizePath(path);
        _files.Remove(normalizedPath);
        _lastWriteTimes.Remove(normalizedPath);
    }
    
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        var normalizedSrc = NormalizePath(sourcePath);
        var normalizedDest = NormalizePath(destinationPath);
        
        if (!_files.ContainsKey(normalizedSrc))
            throw new FileNotFoundException($"Source file not found: {sourcePath}", sourcePath);
        
        if (!overwrite && _files.ContainsKey(normalizedDest))
            throw new IOException($"Destination file already exists: {destinationPath}");
        
        _files[normalizedDest] = (byte[])_files[normalizedSrc].Clone();
        _lastWriteTimes[normalizedDest] = DateTime.UtcNow;
    }
    
    public void MoveFile(string sourcePath, string destinationPath)
    {
        CopyFile(sourcePath, destinationPath, true);
        DeleteFile(sourcePath);
    }
    
    public DateTime GetLastWriteTimeUtc(string path)
    {
        var normalizedPath = NormalizePath(path);
        return _lastWriteTimes.TryGetValue(normalizedPath, out var time) ? time : DateTime.MinValue;
    }
    
    // Directory operations
    public bool DirectoryExists(string path) => _directories.Contains(NormalizePath(path));
    
    public void CreateDirectory(string path)
    {
        var normalizedPath = NormalizePath(path);
        if (_directories.Contains(normalizedPath))
            return;
            
        _directories.Add(normalizedPath);
        
        // Also create parent directories
        var parent = Path.GetDirectoryName(normalizedPath);
        if (!string.IsNullOrEmpty(parent) && !DirectoryExists(parent))
            CreateDirectory(parent);
    }
    
    public void DeleteDirectory(string path, bool recursive = false)
    {
        var normalizedPath = NormalizePath(path);
        
        if (recursive)
        {
            // Delete all files in directory
            var filesToDelete = _files.Keys.Where(f => IsInDirectory(f, normalizedPath)).ToList();
            foreach (var file in filesToDelete)
            {
                _files.Remove(file);
                _lastWriteTimes.Remove(file);
            }
            
            // Delete all subdirectories
            var dirsToDelete = _directories.Where(d => IsInDirectory(d, normalizedPath)).ToList();
            foreach (var dir in dirsToDelete)
                _directories.Remove(dir);
        }
        
        _directories.Remove(normalizedPath);
    }
    
    public string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        var normalizedPath = NormalizePath(path);
        var regex = PatternToRegex(searchPattern);
        
        return _files.Keys
            .Where(f => {
                if (searchOption == SearchOption.TopDirectoryOnly)
                {
                    var dir = Path.GetDirectoryName(f);
                    return string.Equals(dir, normalizedPath, StringComparison.OrdinalIgnoreCase) 
                        && regex.IsMatch(Path.GetFileName(f) ?? "");
                }
                else
                {
                    return IsInDirectory(f, normalizedPath) 
                        && regex.IsMatch(Path.GetFileName(f) ?? "");
                }
            })
            .ToArray();
    }
    
    public string[] GetDirectories(string path)
    {
        var normalizedPath = NormalizePath(path);
        return _directories
            .Where(d => {
                var parent = Path.GetDirectoryName(d);
                return string.Equals(parent, normalizedPath, StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();
    }
    
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        => GetFiles(path, searchPattern, searchOption);
    
    // Helper methods
    string NormalizePath(string path)
    {
        // Normalize separators to forward slash and remove trailing slash
        var normalized = path.Replace("\\", "/").TrimEnd('/');
        
        // Handle empty path
        if (string.IsNullOrEmpty(normalized))
            return "/";
            
        return normalized;
    }
    
    void EnsureDirectoryExists(string path)
    {
        if (!string.IsNullOrEmpty(path) && !DirectoryExists(path))
            CreateDirectory(path);
    }
    
    bool IsInDirectory(string filePath, string directoryPath)
    {
        var normalizedFile = NormalizePath(filePath);
        var normalizedDir = NormalizePath(directoryPath);
        return normalizedFile.StartsWith(normalizedDir + "/", StringComparison.OrdinalIgnoreCase);
    }
    
    static Regex PatternToRegex(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return new Regex(regexPattern, RegexOptions.IgnoreCase);
    }
}
