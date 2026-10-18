# Com.H.Net.Ssh
Wrapper to SSH.NET. Introduces minor new functionality (e.g. SFTP upload / download folders & subfolders, etc..)

## Supported frameworks
One package covers legacy and modern targets:

| Target | Runs on |
| --- | --- |
| `netstandard2.0` | .NET Framework 4.6.2+, Mono, Xamarin, and anything else that consumes .NET Standard 2.0 |
| `net8.0` / `net9.0` / `net10.0` | .NET 8, 9 and 10 |

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
// new Com.H.Net.Ssh.SFtpClient("server_name_or_ip", 22, 
//    new Com.H.Net.Ssh.PrivateKeyFileSettings()
//    {
//        Path = @"C:\path\to\key.pem",
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

The private key should be of a classical format (not OpenSSH format). 
If you happen to have an OpenSSH based private key, you can convert it to classical using the following command:
```bash
ssh-keygen -p -f my_openssh_private_key.pem -m pem -P "my passphrase" -N "my passphrase" -O my_classic_private_key.pem
```

And if you don't have a passphrase, you can use the following command to convert your key:
```bash
ssh-keygen -p -f my_openssh_private_key.pem -m pem -P "" -N "" -O my_classic_private_key.pem
```