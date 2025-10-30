# SSH.NET SftpClient API Reference

**Documentation Source:** https://sshnet.github.io/SSH.NET/api/Renci.SshNet.SftpClient.html  
**Library Version:** SSH.NET 2025.1.0  
**Date:** October 30, 2025

---

## Overview

SSH.NET provides comprehensive async/await support using the Task-based Asynchronous Pattern (TAP). All async methods accept `CancellationToken` parameters and return `Task` or `Task<T>`.

---

## Core Async Methods Available in SftpClient

### Connection Management
- **ConnectAsync(CancellationToken)** - Inherited from BaseClient
- **DisconnectAsync(CancellationToken)** - Inherited from BaseClient

### Directory Operations

#### ChangeDirectoryAsync
```csharp
public Task ChangeDirectoryAsync(string path, CancellationToken cancellationToken = default)
```
Asynchronously changes the current working directory.

**Exceptions:**
- `ArgumentNullException` - path is null
- `SshConnectionException` - Client is not connected
- `SftpPermissionDeniedException` - Permission denied
- `SftpPathNotFoundException` - path not found
- `ObjectDisposedException` - Method called after client disposed

---

#### CreateDirectoryAsync
```csharp
public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
```
Asynchronously creates a remote directory.

**Exceptions:**
- `ArgumentException` - path is null or whitespace
- `SshConnectionException` - Client is not connected
- `SftpPermissionDeniedException` - Permission denied

---

#### DeleteDirectoryAsync
```csharp
public Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken = default)
```
Asynchronously deletes a remote directory.

**Exceptions:**
- `ArgumentException` - path is null or whitespace
- `SshConnectionException` - Client is not connected
- `SftpPathNotFoundException` - path not found
- `SftpPermissionDeniedException` - Permission denied

---

#### ListDirectoryAsync
```csharp
public IAsyncEnumerable<ISftpFile> ListDirectoryAsync(string path, CancellationToken cancellationToken)
```
Asynchronously enumerates files in remote directory.

**Returns:** `IAsyncEnumerable<ISftpFile>` - async stream of files

**Usage:**
```csharp
await foreach (var file in client.ListDirectoryAsync("/path", cancellationToken))
{
    // Process file
}
```

**Exceptions:**
- `ArgumentNullException` - path is null
- `SshConnectionException` - Client is not connected
- `SftpPermissionDeniedException` - Permission denied

---

### File Operations

#### ExistsAsync
```csharp
public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
```
Checks whether file or directory exists.

**Returns:** `Task<bool>` - true if exists, false otherwise

**Exceptions:**
- `ArgumentException` - path is null or whitespace
- `SshConnectionException` - Client is not connected
- `SftpPermissionDeniedException` - Permission denied

---

#### GetAsync
```csharp
public Task<ISftpFile> GetAsync(string path, CancellationToken cancellationToken)
```
Gets reference to remote file or directory.

**Returns:** `Task<ISftpFile>` - file information object

**Exceptions:**
- `SshConnectionException` - Client is not connected
- `SftpPathNotFoundException` - path not found
- `ArgumentNullException` - path is null

---

#### DeleteAsync
```csharp
public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
```
Permanently deletes a file on remote machine.

**Exceptions:**
- `ArgumentNullException` - path is null
- `SshConnectionException` - Client is not connected
- `SftpPathNotFoundException` - path not found

---

#### DeleteFileAsync
```csharp
public Task DeleteFileAsync(string path, CancellationToken cancellationToken)
```
Asynchronously deletes remote file specified by path.

**Exceptions:**
- `ArgumentException` - path is null or whitespace
- `SshConnectionException` - Client is not connected
- `SftpPathNotFoundException` - path not found
- `SftpPermissionDeniedException` - Permission denied

---

#### RenameFileAsync
```csharp
public Task RenameFileAsync(string oldPath, string newPath, CancellationToken cancellationToken)
```
Asynchronously renames remote file from old path to new path.

**Exceptions:**
- `ArgumentNullException` - oldPath or newPath is null
- `SshConnectionException` - Client is not connected
- `SftpPermissionDeniedException` - Permission denied

---

### File Transfer Operations

#### DownloadFileAsync
```csharp
public Task DownloadFileAsync(string path, Stream output, CancellationToken cancellationToken = default)
```
Asynchronously downloads a remote file into a Stream.

**Parameters:**
- `path` - The path to the remote file
- `output` - The Stream to write the file into
- `cancellationToken` - The CancellationToken to observe

**Exceptions:**
- `ArgumentNullException` - output or path is null
- `ArgumentException` - path is empty or whitespace
- `SshConnectionException` - Client is not connected
- `SftpPermissionDeniedException` - Permission denied
- `SftpPathNotFoundException` - path not found

---

#### UploadFileAsync
```csharp
public Task UploadFileAsync(Stream input, string path, CancellationToken cancellationToken = default)
```
Asynchronously uploads a Stream to a remote file path.

**Parameters:**
- `input` - The Stream to write to the remote path
- `path` - The remote file path to write to
- `cancellationToken` - The CancellationToken to observe

**Remarks:** If the remote file already exists, it is overwritten and truncated.

**Exceptions:**
- `ArgumentNullException` - input or path is null
- `ArgumentException` - path is empty or whitespace
- `SshConnectionException` - Client is not connected
- `SftpPermissionDeniedException` - Permission denied

---

#### OpenAsync
```csharp
public Task<SftpFileStream> OpenAsync(string path, FileMode mode, FileAccess access, CancellationToken cancellationToken)
```
Asynchronously opens a SftpFileStream on the specified path, with the specified mode and access.

**Returns:** `Task<SftpFileStream>` - provides access to the specified file

**Usage for Download:**
```csharp
await using var fileStream = await client.OpenAsync(remotePath, FileMode.Open, FileAccess.Read, cancellationToken);
await fileStream.CopyToAsync(localStream, 81920, cancellationToken);
```

**Usage for Upload:**
```csharp
await using var fileStream = await client.OpenAsync(remotePath, FileMode.Create, FileAccess.Write, cancellationToken);
await localStream.CopyToAsync(fileStream, 81920, cancellationToken);
```

**Exceptions:**
- `ArgumentNullException` - path is null
- `SshConnectionException` - Client is not connected

---

### File Attributes

#### GetAttributesAsync
```csharp
public Task<SftpFileAttributes> GetAttributesAsync(string path, CancellationToken cancellationToken)
```
Gets the SftpFileAttributes of the file on the path.

**Returns:** `Task<SftpFileAttributes>` - file attributes

**Exceptions:**
- `ArgumentNullException` - path is null
- `SshConnectionException` - Client is not connected
- `SftpPathNotFoundException` - path not found

---

#### GetStatusAsync
```csharp
public Task<SftpFileSystemInformation> GetStatusAsync(string path, CancellationToken cancellationToken)
```
Asynchronously gets status using statvfs@openssh.com request.

**Returns:** `Task<SftpFileSystemInformation>` - file system information

**Exceptions:**
- `SshConnectionException` - Client is not connected
- `ArgumentNullException` - path is null

---

## Synchronous Methods (Reference)

### File Operations
- `void Delete(string path)`
- `void DeleteDirectory(string path)`
- `void DeleteFile(string path)`
- `bool Exists(string path)`
- `ISftpFile Get(string path)`
- `void RenameFile(string oldPath, string newPath)`
- `void RenameFile(string oldPath, string newPath, bool isPosix)`

### Directory Operations
- `void CreateDirectory(string path)`
- `void ChangeDirectory(string path)`
- `IEnumerable<ISftpFile> ListDirectory(string path, Action<int>? listCallback = null)`

### File Transfer
- `void DownloadFile(string path, Stream output, Action<ulong>? downloadCallback = null)`
- `void UploadFile(Stream input, string path, Action<ulong>? uploadCallback = null)`
- `void UploadFile(Stream input, string path, bool canOverride, Action<ulong>? uploadCallback = null)`

### Stream Operations
- `SftpFileStream Open(string path, FileMode mode)`
- `SftpFileStream Open(string path, FileMode mode, FileAccess access)`
- `SftpFileStream OpenRead(string path)`
- `SftpFileStream OpenWrite(string path)`
- `SftpFileStream Create(string path)`
- `SftpFileStream Create(string path, int bufferSize)`

### Text File Operations
- `string ReadAllText(string path)`
- `string ReadAllText(string path, Encoding encoding)`
- `string[] ReadAllLines(string path)`
- `string[] ReadAllLines(string path, Encoding encoding)`
- `IEnumerable<string> ReadLines(string path)`
- `IEnumerable<string> ReadLines(string path, Encoding encoding)`
- `byte[] ReadAllBytes(string path)`
- `void WriteAllText(string path, string contents)`
- `void WriteAllText(string path, string contents, Encoding encoding)`
- `void WriteAllLines(string path, IEnumerable<string> contents)`
- `void WriteAllLines(string path, string[] contents)`
- `void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding)`
- `void WriteAllLines(string path, string[] contents, Encoding encoding)`
- `void WriteAllBytes(string path, byte[] bytes)`
- `void AppendAllText(string path, string contents)`
- `void AppendAllText(string path, string contents, Encoding encoding)`
- `void AppendAllLines(string path, IEnumerable<string> contents)`
- `void AppendAllLines(string path, IEnumerable<string> contents, Encoding encoding)`

### Attributes
- `SftpFileAttributes GetAttributes(string path)`
- `void SetAttributes(string path, SftpFileAttributes fileAttributes)`

---

## Legacy APM (Asynchronous Programming Model) Methods

**Note:** These use the older Begin/End pattern. Use TAP (Task-based) methods instead.

- `IAsyncResult BeginDownloadFile(string path, Stream output, ...)`
- `void EndDownloadFile(IAsyncResult asyncResult)`
- `IAsyncResult BeginUploadFile(Stream input, string path, ...)`
- `void EndUploadFile(IAsyncResult asyncResult)`
- `IAsyncResult BeginListDirectory(string path, ...)`
- `IEnumerable<ISftpFile> EndListDirectory(IAsyncResult asyncResult)`

---

## Common Exception Types

### SshConnectionException
Thrown when client is not connected. Always check `IsConnected` or call `Connect()`/`ConnectAsync()` before operations.

### SftpPathNotFoundException
Thrown when the specified path was not found on the remote host.

### SftpPermissionDeniedException
Thrown when permission to perform the operation was denied by the remote host, or an SSH command was denied by the server.

### ArgumentNullException
Thrown when a required parameter is null.

### ArgumentException
Thrown when a parameter is invalid (e.g., empty string or whitespace for paths).

### ObjectDisposedException
Thrown when the method is called after the client has been disposed.

---

## Best Practices for Async Implementation

### 1. Use Native Async Methods
✅ **DO:**
```csharp
await using var stream = await client.OpenAsync(path, FileMode.Open, FileAccess.Read, cancellationToken);
await stream.CopyToAsync(destination, 81920, cancellationToken);
```

❌ **DON'T:**
```csharp
await Task.Run(() => client.Open(path, FileMode.Open, FileAccess.Read));
```

### 2. Use IAsyncEnumerable for ListDirectory
```csharp
public async IAsyncEnumerable<SFtpFileInfo> ListFilesAsync(
    string remotePath, 
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    AutoConnect();
    await foreach (var file in Client.ListDirectoryAsync(remotePath, cancellationToken))
    {
        if (file.IsRegularFile)
            yield return new SFtpFileInfo(file);
    }
}
```

### 3. Proper Stream Buffer Size
Use **81920 bytes** (80 KB) for optimal network I/O:
```csharp
await sourceStream.CopyToAsync(destinationStream, 81920, cancellationToken);
```

### 4. Always Pass CancellationToken
```csharp
public async Task<bool> ExistAsync(string remotePath, CancellationToken cancellationToken = default)
{
    AutoConnect();
    return await Client.ExistsAsync(remotePath, cancellationToken);
}
```

### 5. Use Modern C# Features in .NET 8/9
- `await using` for async disposal
- `await foreach` for async enumeration
- `File.ReadAllTextAsync` / `File.WriteAllTextAsync`
- `ConfigureAwait(false)` is not needed in modern .NET (unless in library code)

---

## Implementation Patterns for Wrapper Methods

### Simple Pass-Through
```csharp
public async Task<bool> ExistAsync(string remotePath, CancellationToken cancellationToken = default)
{
    AutoConnect();
    return await Client.ExistsAsync(remotePath, cancellationToken);
}
```

### Download with Stream Management
```csharp
public async Task DownloadAsync(
    string remotePath, 
    string localPath, 
    CancellationToken cancellationToken = default)
{
    AutoConnect();
    await using var fileStream = File.Create(localPath);
    await Client.DownloadFileAsync(remotePath, fileStream, cancellationToken);
}
```

### Download to Stream (User-Managed)
```csharp
public async Task DownloadAsync(
    string remotePath, 
    Stream destination, 
    bool? closeStream = null, 
    CancellationToken cancellationToken = default)
{
    AutoConnect();
    bool shouldClose = closeStream ?? AutoCloseStreams;
    
    try
    {
        await Client.DownloadFileAsync(remotePath, destination, cancellationToken);
    }
    finally
    {
        if (shouldClose)
            await destination.DisposeAsync();
    }
}
```

### Upload with Stream Management
```csharp
public async Task UploadAsync(
    string localPath, 
    string remotePath, 
    Func<ulong, bool>? uploadCallback = null, 
    CancellationToken cancellationToken = default)
{
    AutoConnect();
    await using var fileStream = File.OpenRead(localPath);
    
    await using var remoteStream = await Client.OpenAsync(
        remotePath, 
        FileMode.Create, 
        FileAccess.Write, 
        cancellationToken);
    
    await fileStream.CopyToAsync(remoteStream, 81920, cancellationToken);
}
```

### List Directory with Filtering
```csharp
public async IAsyncEnumerable<SFtpFileInfo> ListFilesAsync(
    string remotePath, 
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    AutoConnect();
    
    await foreach (var item in Client.ListDirectoryAsync(remotePath, cancellationToken))
    {
        if (item.IsRegularFile)
            yield return new SFtpFileInfo(item);
    }
}
```

### Download As String
```csharp
public async Task<string> DownloadAsStringAsync(
    string remotePath, 
    Encoding? encoding = null, 
    Func<ulong, bool>? downloadCallback = null, 
    CancellationToken cancellationToken = default)
{
    AutoConnect();
    encoding ??= Encoding.UTF8;
    
    using var memoryStream = new MemoryStream();
    await Client.DownloadFileAsync(remotePath, memoryStream, cancellationToken);
    
    memoryStream.Position = 0;
    using var reader = new StreamReader(memoryStream, encoding);
    return await reader.ReadToEndAsync(cancellationToken);
}
```

---

## Properties

### BufferSize
```csharp
public int BufferSize { get; set; }
```
Gets or sets the size of the buffer for read and write operations.

### OperationTimeout
```csharp
public TimeSpan OperationTimeout { get; set; }
```
Gets or sets the operation timeout.

### WorkingDirectory
```csharp
public string WorkingDirectory { get; }
```
Gets the current working directory.

### ProtocolVersion
```csharp
public int ProtocolVersion { get; }
```
Gets the SFTP protocol version.

### IsConnected
```csharp
public bool IsConnected { get; }
```
Gets a value indicating whether this client is connected to the server.

---

## Connection Management

### Properties from BaseClient
- `ConnectionInfo` - Gets the connection info
- `IsConnected` - Gets whether the client is connected
- `KeepAliveInterval` - Gets or sets keep-alive interval

### Methods from BaseClient
- `void Connect()`
- `Task ConnectAsync(CancellationToken cancellationToken = default)`
- `void Disconnect()`
- `Task DisconnectAsync(CancellationToken cancellationToken = default)`
- `void Dispose()`

---

## Notes for Implementation

1. **AutoConnect Pattern:** Create both `AutoConnect()` for sync and `AutoConnectAsync()` for async methods
2. **Stream Management:** Respect `AutoCloseStreams` property or accept nullable `closeStream` parameter
3. **Progress Callbacks:** SSH.NET's async methods don't support progress callbacks - would need to implement custom progress reporting
4. **CancellationToken:** Always include and pass through to SSH.NET methods
5. **Error Handling:** All SSH.NET exceptions should bubble up (don't swallow them)
6. **Encoding:** Default to UTF8, but allow override via parameters
7. **File Modes:** For uploads, use `FileMode.Create` to overwrite; for appends, use `FileMode.Append`

---

## Version Information

- **Package:** SSH.NET 2025.1.0
- **Target Frameworks:** .NET 8.0, .NET 9.0
- **Language Version:** C# latest
- **Platform:** netstandard2.0, netstandard2.1, net6.0, net8.0

---

## Additional Resources

- **Official Documentation:** https://sshnet.github.io/SSH.NET/
- **GitHub Repository:** https://github.com/sshnet/SSH.NET
- **NuGet Package:** https://www.nuget.org/packages/SSH.NET/
