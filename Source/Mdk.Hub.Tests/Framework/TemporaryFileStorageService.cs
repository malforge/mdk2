using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mdk.Hub.Features.Storage;

public sealed class TemporaryFileStorageService : IFileStorageService, IDisposable
{
    readonly string _appData;
    readonly string _documents;
    readonly string _localAppData;
    readonly string _root;
    readonly string _seData;
    readonly string _temp;

    public TemporaryFileStorageService()
    {
        _root = Path.Combine(Path.GetTempPath(), $"MdkHubTests_{Guid.NewGuid():N}");
        _appData = Path.Combine(_root, "appdata");
        _localAppData = Path.Combine(_root, "localappdata");
        _temp = Path.Combine(_root, "temp");
        _documents = Path.Combine(_root, "documents");
        _seData = Path.Combine(_root, "spaceengineers");

        Directory.CreateDirectory(_appData);
        Directory.CreateDirectory(_localAppData);
        Directory.CreateDirectory(_temp);
        Directory.CreateDirectory(_documents);
        Directory.CreateDirectory(_seData);
    }

    public string GetApplicationDataPath(params string[] subPaths) => subPaths.Length == 0 ? _appData : Path.Combine([_appData, .. subPaths]);
    public string GetLocalApplicationDataPath(params string[] subPaths) => subPaths.Length == 0 ? _localAppData : Path.Combine([_localAppData, .. subPaths]);
    public string GetTempPath(params string[] subPaths) => subPaths.Length == 0 ? _temp : Path.Combine([_temp, .. subPaths]);
    public string GetDocumentsPath(params string[] subPaths) => subPaths.Length == 0 ? _documents : Path.Combine([_documents, .. subPaths]);
    public string GetSpaceEngineersDataPath(params string[] subPaths) => subPaths.Length == 0 ? _seData : Path.Combine([_seData, .. subPaths]);

    public bool FileExists(string path) => File.Exists(path);
    public string ReadAllText(string path) => File.ReadAllText(path);
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) => File.ReadAllTextAsync(path, cancellationToken);
    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);
    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default) => File.ReadAllBytesAsync(path, cancellationToken);
    public Stream OpenRead(string path) => File.OpenRead(path);

    public void WriteAllText(string path, string contents)
    {
        EnsureDirectory(path);
        File.WriteAllText(path, contents);
    }

    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        EnsureDirectory(path);
        return File.WriteAllTextAsync(path, contents, cancellationToken);
    }

    public void AppendAllText(string path, string contents)
    {
        EnsureDirectory(path);
        File.AppendAllText(path, contents);
    }

    public Task AppendAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        EnsureDirectory(path);
        return File.AppendAllTextAsync(path, contents, cancellationToken);
    }

    public void WriteAllBytes(string path, byte[] bytes)
    {
        EnsureDirectory(path);
        File.WriteAllBytes(path, bytes);
    }

    public Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
    {
        EnsureDirectory(path);
        return File.WriteAllBytesAsync(path, bytes, cancellationToken);
    }

    public void DeleteFile(string path) => File.Delete(path);
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite = false) => File.Copy(sourcePath, destinationPath, overwrite);
    public void MoveFile(string sourcePath, string destinationPath) => File.Move(sourcePath, destinationPath);
    public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public void DeleteDirectory(string path, bool recursive = false) => Directory.Delete(path, recursive);
    public string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => Directory.GetFiles(path, searchPattern, searchOption);
    public string[] GetDirectories(string path) => Directory.GetDirectories(path);
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => Directory.EnumerateFiles(path, searchPattern, searchOption);

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    static void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }
}
