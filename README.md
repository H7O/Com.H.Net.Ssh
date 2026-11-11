# Com.H.Net.Ssh
Wrapper to SSH.NET. Introduces minor new functionality (e.g. SFTP upload / download folders & subfolders, etc..)

## Supported frameworks
One package covers legacy and modern targets:

| Target | Runs on |
| --- | --- |
| `netstandard2.0` | .NET Framework 4.6.2+, Mono, Xamarin, and anything else that consumes .NET Standard 2.0 |
| `net8.0` / `net9.0` / `net10.0` / `net11.0` | .NET 8, 9, 10 and 11 |

Every target exposes the same API, sync and async alike. On `netstandard2.0` the async surface is
backed by the `Microsoft.Bcl.AsyncInterfaces` package, which NuGet pulls in automatically; the modern
targets use the built-in types and take no extra dependency.

Before version 10.1.0 this was split across two version lines of the same package id — 2.x built for
`netstandard2.0` and was sync-only, 10.x built for .NET 8+ and had the async methods. From 10.1.0
onwards there is one line, so a .NET Framework project can install the latest version and get async.

## How to install
Best to install it via NuGet package manager [https://www.nuget.org/packages/Com.H.Net.Ssh](https://www.nuget.org/packages/Com.H.Net.Ssh)

## How to use
Here is a sample example
```c#
Com.H.Net.Ssh.SFtpClient sFtpClient = 
    new Com.H.Net.Ssh.SFtpClient("server_name_or_ip", 22, "user_id", "pwd");
// or you can use the constructor with a private key instead of a password
// new Com.H.Net.Ssh.SFtpClient("server_name_or_ip", 22, "user_id",
//    new Com.H.Net.Ssh.PrivateKeyFileSettings()
//    {
//        Path = @"C:\path\to\private_key",
//        Passphrase = "my passphrase" 
//    });

// single file upload (every method also has an async version - see below)
sFtpClient.Upload("c:/test/files_to_upload/some_file.txt", 
    "remote_folder/some_file.txt");

// single folder (and its subfolders) upload
sFtpClient.Upload("c:/test/files_to_upload/",
    "remote_folder/");

// upload from input stream
sFtpClient.Upload(File.OpenRead("c:/test/files_to_upload/some_file.txt"),
    "remote_folder/some_file.txt");

// download a file
sFtpClient.Download("remote_folder/some_file.txt", 
    "c:/test/downloaded_files/somefile.txt");

// download to an output stream
sFtpClient.Download("remote_folder/some_file.txt", 
    File.OpenWrite("c:/test/downloaded_files/somefile.txt"));

// download a folder (and its subfolders)
sFtpClient.Download("remote_folder/", 
    "c:/test/files_to_download/");

// rename (move) a remote file; on most servers it fails if the new path exists
sFtpClient.Rename("remote_folder/some_file.tmp",
    "remote_folder/some_file.txt");
```

### Async
Every method above has an async counterpart taking an optional `CancellationToken`. These work on
.NET Framework 4.6.2+ as well, not just .NET 8+.

```c#
await using var sFtpClient =
    new Com.H.Net.Ssh.SFtpClient("server_name_or_ip", 22, "user_id", "pwd");

await sFtpClient.UploadAsync("c:/test/files_to_upload/some_file.txt",
    "remote_folder/some_file.txt", cancellationToken: token);

await sFtpClient.DownloadAsync("remote_folder/", "c:/test/files_to_download/", token);

var text = await sFtpClient.DownloadAsStringAsync("remote_folder/some_file.txt",
    cancellationToken: token);

// caller disposes the returned stream
await using var stream = await sFtpClient.DownloadAsStreamAsync("remote_folder/some_file.txt", token);

foreach (var f in await sFtpClient.ListFilesAsync("remote_folder/", token))
    Console.WriteLine(f.FullPath);
```

On `netstandard2.0`, `await using` needs C# 8 or later in the consuming project (`<LangVersion>latest</LangVersion>`);
plain `using` plus an explicit `await client.DisposeAsync()` works otherwise.

## File sizes
`ListFiles` and `GetFileInfo` (and their async versions) fill in each entry's `Size` in bytes, from
version 11.0.0 on. Before that it was always zero.

```c#
foreach (var f in await sFtpClient.ListFilesAsync("remote_folder/", token))
    Console.WriteLine($"{f.Name}: {(f.IsDirectory ? "folder" : f.Size + " bytes")}");
```

## Checking the server's host key
This check is new in version 11.0.0, and it is optional. Leave `HostKeyFingerprint` unset and the client connects as it did in every
earlier version, whatever key the server shows. Nothing has to be looked up before a first login.

An SSH server proves who it is with its host key. Unless you say which key to expect, any server
that answers at the address is accepted, and it receives your password and your files. Set
`HostKeyFingerprint` and the client refuses any server that shows another key, before the login is sent:

```c#
var sFtpClient = new Com.H.Net.Ssh.SFtpClient("server_name_or_ip", 22, "user_id", "pwd")
{
    HostKeyFingerprint = "SHA256:DA04nSicK5AmDlNtTpfXp2RKDOpqCtEb3cXppB5Mfp8"
};
```

The fingerprint is the one OpenSSH shows the first time you connect to a server ("ED25519 key
fingerprint is SHA256:..."), and the one `ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub` prints on
the server itself. A server that shows another key makes every method throw an `SshConnectionException`
that names both fingerprints.

### Trusting a server the first time
You don't need to know the fingerprint in advance. After a connection, `ServerHostKeyFingerprint`
holds the fingerprint the server showed. Store it after the first connection and set
`HostKeyFingerprint` to it from then on. The first connection then accepts the server's key, and
every later one is held to that same key:

```c#
var saved = File.Exists("server.fingerprint") ? File.ReadAllText("server.fingerprint") : null;

var sFtpClient = new Com.H.Net.Ssh.SFtpClient("server_name_or_ip", 22, "user_id", "pwd")
{
    HostKeyFingerprint = saved   // null the first time, so the first connection accepts the key
};

sFtpClient.Upload("c:/test/files_to_upload/some_file.txt", "remote_folder/some_file.txt");

if (saved == null)
    File.WriteAllText("server.fingerprint", sFtpClient.ServerHostKeyFingerprint);
```

This is what OpenSSH does with its `known_hosts` file.

## Private keys
A private key works in the format `ssh-keygen` writes today (OpenSSH's own, which starts with
`-----BEGIN OPENSSH PRIVATE KEY-----`) and in the older PEM format. This is checked with ed25519, RSA
and ECDSA keys, with and without a passphrase.

Only if you are on an old version of this package that refuses an OpenSSH-format key, convert the key
to PEM (use `""` for both passphrases if the key has none):
```bash
ssh-keygen -p -f my_private_key -m pem -P "my passphrase" -N "my passphrase"
```