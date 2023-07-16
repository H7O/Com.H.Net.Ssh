# Com.H.Net.Ssh
Wrapper to Renci.SshNet. Introduces minor new functionality (e.g. SFTP upload / download folders & subfolders, etc..)

## How to install
Best to install it via NuGet package manager [https://www.nuget.org/packages/Com.H.Net.Ssh](https://www.nuget.org/packages/Com.H.Net.Ssh)

## How to use
Here is a sample example
```c#
Com.H.Net.Ssh.SFtpClient sFtpClient = 
    new Com.H.Net.Ssh.SFtpClient("server_name_or_ip", 22, "user_id", "pwd");
// or you can use the constructor with the private key
// new Com.H.Net.Ssh.SFtpClient("server_name_or_ip", 22, 
//        "user_id", new PrivateKeyFile("path/to/key"));


// single file upload
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
```

The private key should be of a classical format (not OpenSSH format). You can convert the key using the following command:
```bash
ssh-keygen -p -f my_openssh_private_key.pem -m pem -P "my passphrase" -N "my passphrase" -O my_classic_private_key.pem
```

And if you don't have a passphrase, you can use the following command to convert your key:
```bash
ssh-keygen -p -f my_openssh_private_key.pem -m pem -P "" -N "" -O my_classic_private_key.pem
```