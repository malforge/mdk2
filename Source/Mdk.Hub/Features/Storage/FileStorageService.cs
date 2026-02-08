using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Storage;

/// <summary>
///     Production implementation of IFileStorageService that uses the real filesystem.
/// </summary>
[Singleton<IFileStorageService>]
public class FileStorageService : IFileStorageService
{
    readonly string _appData;
    readonly string _localAppData;
    readonly string _temp;
    readonly string _documents;
    readonly string _seData;
    
    /// <summary>
    ///     Initializes a new instance of FileStorageService.
    /// </summary>
    public FileStorageService()
    {
        _appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MDK2");
        _localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MDK2");
        _temp = Path.GetTempPath();
        _documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _seData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers");
    }
    
    /// <inheritdoc />
    public string GetApplicationDataPath(params string[] subPaths) 
        => subPaths.Length == 0 ? _appData : Path.Combine([_appData, .. subPaths]);
    
    /// <inheritdoc />
    public string GetLocalApplicationDataPath(params string[] subPaths) 
        => subPaths.Length == 0 ? _localAppData : Path.Combine([_localAppData, .. subPaths]);
    
    /// <inheritdoc />
    public string GetTempPath(params string[] subPaths) 
        => subPaths.Length == 0 ? _temp : Path.Combine([_temp, .. subPaths]);
    
    /// <inheritdoc />
    public string GetDocumentsPath(params string[] subPaths) 
        => subPaths.Length == 0 ? _documents : Path.Combine([_documents, .. subPaths]);
    
    /// <inheritdoc />
    public string GetSpaceEngineersDataPath(params string[] subPaths) 
        => subPaths.Length == 0 ? _seData : Path.Combine([_seData, .. subPaths]);
    
    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(path);
    /// <inheritdoc />
    public string ReadAllText(string path) => File.ReadAllText(path);
    /// <inheritdoc />
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) 
        => File.ReadAllTextAsync(path, cancellationToken);
    /// <inheritdoc />
    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);
    /// <inheritdoc />
    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default) 
        => File.ReadAllBytesAsync(path, cancellationToken);
    /// <inheritdoc />
    public Stream OpenRead(string path) => File.OpenRead(path);
    
    /// <inheritdoc />
    public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default) 
        => File.WriteAllTextAsync(path, contents, cancellationToken);
    /// <inheritdoc />
    public void AppendAllText(string path, string contents) => File.AppendAllText(path, contents);
    /// <inheritdoc />
    public Task AppendAllTextAsync(string path, string contents, CancellationToken cancellationToken = default) 
        => File.AppendAllTextAsync(path, contents, cancellationToken);
    /// <inheritdoc />
    public void WriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);
    /// <inheritdoc />
    public Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default) 
        => File.WriteAllBytesAsync(path, bytes, cancellationToken);
    
    /// <inheritdoc />
    public void DeleteFile(string path) => File.Delete(path);
    /// <inheritdoc />
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite = false) 
        => File.Copy(sourcePath, destinationPath, overwrite);
    /// <inheritdoc />
    public void MoveFile(string sourcePath, string destinationPath) 
        => File.Move(sourcePath, destinationPath);
    /// <inheritdoc />
    public DateTime GetLastWriteTimeUtc(string path) 
        => File.GetLastWriteTimeUtc(path);
    
    /// <inheritdoc />
    public bool DirectoryExists(string path) => Directory.Exists(path);
    /// <inheritdoc />
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    /// <inheritdoc />
    public void DeleteDirectory(string path, bool recursive = false) 
        => Directory.Delete(path, recursive);
    /// <inheritdoc />
    public string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) 
        => Directory.GetFiles(path, searchPattern, searchOption);
    /// <inheritdoc />
    public string[] GetDirectories(string path) 
        => Directory.GetDirectories(path);
    /// <inheritdoc />
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) 
        => Directory.EnumerateFiles(path, searchPattern, searchOption);
}
