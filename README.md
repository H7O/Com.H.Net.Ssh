# Com.H.Net.Ssh
Wrapper to Renci.SshNet. Introduces minor new functionality (e.g. SFTP upload a folder, etc..)

## How to install
Best to install it via NuGet package manager [https://www.nuget.org/packages/Com.H.Net.Ssh](https://www.nuget.org/packages/Com.H.Net.Ssh)

## How to use
Here is a sample example
```c#
Com.H.Net.Ssh.SFtpClient sFtpClient = 
    new Com.H.Net.Ssh.SFtpClient("server_name_or_ip", 22, "user_id", "pwd");

// single file upload
sFtpClient.Upload("c:/test/files_to_upload/some_file.txt", 
    "remote_folder/some_file.txt");
// single folder upload (uploads its subfolders as well)
sFtpClient.Upload("c:/test/files_to_upload",
    "remote_folder");
```

