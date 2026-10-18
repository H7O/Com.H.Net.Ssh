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
// new Com.H.Net.Ssh.SFtpClient("server_name_or_ip", 22, 
//    new Com.H.Net.Ssh.PrivateKeyFileSettings()
//    {
//        Path = @"C:\path\to\key.pem",
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

The private key should be of a classical format (not OpenSSH format). 
If you happen to have an OpenSSH based private key, you can convert it to classical using the following command:
```bash
ssh-keygen -p -f my_openssh_private_key.pem -m pem -P "my passphrase" -N "my passphrase" -O my_classic_private_key.pem
```

And if you don't have a passphrase, you can use the following command to convert your key:
```bash
ssh-keygen -p -f my_openssh_private_key.pem -m pem -P "" -N "" -O my_classic_private_key.pem
```