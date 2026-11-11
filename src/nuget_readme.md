# Com.H.Net.Ssh
Wrapper to Renci.SshNet. Introduces minor new functionality (e.g. SFTP upload / download folders & subfolders, etc..)

Targets `netstandard2.0` (.NET Framework 4.6.2+), `net8.0`, `net9.0` and `net10.0` from one package,
with the same sync and async API on every one of them.

Kindly visit the project's github page for documentation [https://github.com/H7O/Com.H.Net.Ssh](https://github.com/H7O/Com.H.Net.Ssh)

## Installation
```bash
dotnet add package Com.H.Net.Ssh
```

## Usage
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

// single file upload (every method also has an async version, e.g. UploadAsync)
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

## Checking the server's host key
This check is optional. Left unset, the client connects as it did in every earlier version.

Unless you say which host key to expect, any server that answers at the address is accepted, and it
receives your password and your files. Set `HostKeyFingerprint` and the client refuses any server that
shows another key, before the login is sent:

```c#
var sFtpClient = new Com.H.Net.Ssh.SFtpClient("server_name_or_ip", 22, "user_id", "pwd")
{
    HostKeyFingerprint = "SHA256:DA04nSicK5AmDlNtTpfXp2RKDOpqCtEb3cXppB5Mfp8"
};
```

You don't need to know the fingerprint in advance. After a connection, `ServerHostKeyFingerprint`
holds the fingerprint the server showed. Store it after the first connection and set
`HostKeyFingerprint` to it from then on: the first connection accepts the server's key, and every
later one is held to that same key.

## Private keys
A private key works in the format `ssh-keygen` writes today (OpenSSH's own) and in the older PEM
format, with or without a passphrase.
