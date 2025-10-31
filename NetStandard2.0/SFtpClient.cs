using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Com.H.Net.Ssh
{
    /// <summary>
    /// Requires SSH.NET API Renci.SshNet.dll (https://github.com/sshnet/SSH.NET). 
    /// If this library is installed via NuGet, the required Renci.SshNet.dll dependency will be automatically added to the project.
    /// </summary>
    public class SFtpClient : IDisposable
    {
        /// <summary>
        /// Global default for automatically closing streams across all SFtpClient instances.
        /// This is checked when an instance's AutoCloseStreams property is null.
        /// Default is false. Set to true to enable auto-close globally across all instances.
        /// Can be overridden per instance by setting AutoCloseStreams, or per method call using closeStream parameter.
        /// </summary>
        public static bool GlobalAutoCloseStreams { get; set; } = false;
        
        /// <summary>
        /// Disables automatic client disconnection after upload/download operations.
        /// Default is false. The library will disconnect automatically after each operation.
        /// The library auto connects before each operation.
        /// </summary>
        public bool KeepConnectionOpen {get;set;} = false;
        
        /// <summary>
        /// Automatically closes streams passed to Upload/Download methods after the operation completes.
        /// Default is null. When null, uses GlobalAutoCloseStreams value (which defaults to false).
        /// When false, the caller is responsible for managing stream lifetime.
        /// Can be overridden per method call using the closeStream parameter.
        /// </summary>
        public bool? AutoCloseStreams {get;set;} = null;
        
        private Renci.SshNet.SftpClient c;
        public Renci.SshNet.SftpClient Client
        {
            get
            {
                if (c == null)
                {
                    if (this.PrivateKey != null)
                    {
                        if (!string.IsNullOrEmpty(this.PrivateKey.Passphrase))
                        {
                            c = new Renci.SshNet.SftpClient(
                                this.ServerAddress,
                                this.Port,
                                this.UId,
                                new PrivateKeyFile(
                                    this.PrivateKey.Path,
                                    this.PrivateKey.Passphrase));
                        }
                        else
                        {
                            c = new Renci.SshNet.SftpClient(
                                this.ServerAddress,
                                this.Port,
                                this.UId,
                                new PrivateKeyFile(
                                    this.PrivateKey.Path));
                        }
                    }
                    else
                        c = new Renci.SshNet.SftpClient(
                            this.ServerAddress,
                            this.Port,
                            this.UId,
                            this.Pwd);
                }
                return c;
            }
        }
        private bool AutoConnect()
        {
            if (this.Client.IsConnected) return false;
            this.Client.Connect();
            return true;
        }
        /// <summary>
        /// Optional, useful only if KeepConnectionOpen is set to true.
        /// The default library behaviour (when KeepConnectionOpen is false) is to disconnect automatically after each operation.
        /// </summary>
        public void Disconnect()
        {
            if (!this.Client.IsConnected) 
                return;
            this.Client.Disconnect();
        }
        public string UId { get; set; }
        public string Pwd { get; set; }
        public string ServerAddress { get; set; }
        public int Port { get; set; } = 22;

        public PrivateKeyFileSettings PrivateKey { get; set; }
        

        private const string FolderSplitterRegex = @".+?[/|\\]";
        public SFtpClient(string serverAddress, string uid, string pwd) =>
            (this.ServerAddress, this.UId, this.Pwd)
            = (serverAddress, uid, pwd);

        public SFtpClient(string serverAddress, int port, string uid, string pwd) =>
            (this.ServerAddress, this.Port, this.UId, this.Pwd)
            = (serverAddress, port, uid, pwd);

        public SFtpClient(string serverAddress, string uid, PrivateKeyFileSettings privateKeyFileSettings) =>
            (this.ServerAddress, this.UId, this.PrivateKey)
            = (serverAddress, uid, privateKeyFileSettings);

        public SFtpClient(string serverAddress, int port, string uid, PrivateKeyFileSettings privateKeyFileSettings) =>
            (this.ServerAddress, this.Port, this.UId, this.PrivateKey)
            = (serverAddress, port, uid, privateKeyFileSettings);
            

        #region Exist
        public bool Exist(string remotePath) => this.ExistInternal(remotePath, this.KeepConnectionOpen);
        private bool ExistInternal(string remotePath, bool? keepConnectionOpen)
        {
            if (keepConnectionOpen is null)
                keepConnectionOpen = this.KeepConnectionOpen;
            try
            {
                this.AutoConnect();
                return this.Client.Exists(remotePath);
            }
            catch { throw; }
            finally
            {
                try
                {
                    if (keepConnectionOpen != true)
                        this.Disconnect();
                }
                catch { }
            }
        }
        #endregion

        
        #region download

        public void Download(string remotePath, string localPath) => this.DownloadInternal(remotePath, localPath, this.KeepConnectionOpen);
        private void DownloadInternal(string remotePath, 
            string localPath,
            bool? keepConnectionOpen=null)
        {
            if (keepConnectionOpen is null) keepConnectionOpen = this.KeepConnectionOpen;

            if (string.IsNullOrWhiteSpace(remotePath)) throw new Exception("empty remotePath");
            if (string.IsNullOrWhiteSpace(localPath)) throw new Exception("empty localPath");
            if (!Directory.Exists(Path.GetDirectoryName(localPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(localPath));

            try 
            {
				var fInfo = this.GetFileInfoInternal(remotePath, true);

				if (fInfo.IsDirectory)
				{
					if (File.Exists(localPath))
                        File.Delete(localPath);
						// throw new Exception("remotePath is a directory whereas localPath is a file, cannot download a directory into a file");
					if (!Directory.Exists(localPath))
						Directory.CreateDirectory(localPath);

					foreach (var f in this.ListFilesInternal(remotePath, true))
						this.DownloadInternal(f.FullPath,
							Path.Combine(localPath, f.Name), true);
					return;
				}

				if (File.Exists(localPath)) File.Delete(localPath);
				using (var fs = new FileStream(localPath, FileMode.Create))
				{
					this.DownloadInternal(remotePath, fs, true);
				}
			}
			catch { throw; }
            finally
            {
                try
                {
                    if (keepConnectionOpen != true)
                        this.Disconnect();
                }
                catch { }
            }
        }
        /// <summary>
        /// Downloads a remote file to a stream.
        /// </summary>
        /// <param name="remotePath">Remote file path to download</param>
        /// <param name="localStream">Output stream to write to</param>
        /// <param name="closeStream">If true, closes the stream after download. If false, leaves it open. If null, uses the AutoCloseStreams property value.</param>
        public void Download(string remotePath, Stream localStream, bool? closeStream = null) 
            => this.DownloadInternal(remotePath, localStream, this.KeepConnectionOpen, closeStream);
        private void DownloadInternal(string remotePath, 
            Stream output, 
            bool? keepConnectionOpen=null,
            bool? closeStream = null)
        {
            if (keepConnectionOpen is null) keepConnectionOpen = this.KeepConnectionOpen;
            if (closeStream is null) closeStream = this.AutoCloseStreams ?? GlobalAutoCloseStreams;
            
            try
            {
                this.AutoConnect();
                this.Client.DownloadFile(remotePath, output);
            }
            catch { throw; }
            finally
            {
                try
                {
                    if (closeStream == true)
                        output?.Dispose();
                }
                catch { }
                try
                {
                    if (keepConnectionOpen != true) this.Disconnect();
                }
                catch { }
            }
        }


        public string DownloadAsString(
            string remotePath,
            Encoding encoding = null,
            Func<string, string> postProcess = null
        ) => 
            this.DownloadAsStringInternal(
                remotePath, 
                encoding,
                postProcess,
                this.KeepConnectionOpen);
        private string DownloadAsStringInternal(
            string remoteFilePath,
            Encoding encoding = null,
            Func<string, string> postProcess = null,
            bool? keepConnectionOpen = null
            )
        {
            if (keepConnectionOpen is null) keepConnectionOpen = this.KeepConnectionOpen;
            string tempPath = null;
            string tempPathBackup = null;
            try
            {
                using (var f = File.OpenWrite(tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp")))
                    this.DownloadInternal(remoteFilePath, f, keepConnectionOpen);
                if (postProcess != null)
                    tempPath = postProcess(tempPathBackup = tempPath);
                return encoding == null ? File.ReadAllText(tempPath) : File.ReadAllText(tempPath, encoding);
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    if (tempPath != null)
                        File.Delete(tempPath);
                }
                catch { }
                try
                {
                    if (tempPathBackup != null)
                        File.Delete(tempPathBackup);
                }
                catch { }
            }

        }

        #endregion
        
        #region delete
        public void Delete(string remotePath) => this.DeleteInternal(remotePath, this.KeepConnectionOpen);
        private void DeleteInternal(string remotePath, bool? keepConnectionOpen = null)
        {
            if (keepConnectionOpen is null) keepConnectionOpen = this.KeepConnectionOpen;
            try
            {
                this.AutoConnect();
                this.Client.Delete(remotePath);
            }
            catch { throw; }
            finally
            {
                try
                {
                    if (keepConnectionOpen != true) this.Disconnect();
                }
                catch { }
            }
        }
        #endregion

        #region upload
        public void Upload(
            string localPath, 
            string remotePath,
            Func<string, string> preProcess = null
            ) => 
        this.UploadInternal(
            localPath, 
            remotePath, 
            preProcess,
            this.KeepConnectionOpen);
        private void UploadInternal(string localPath, 
            string remotePath, 
            Func<string, string> preProcess = null, 
            bool? keepConnectionOpen=null)
        {
            if (keepConnectionOpen is null) keepConnectionOpen = this.KeepConnectionOpen;
            if (string.IsNullOrEmpty(localPath)) throw new ArgumentNullException(nameof(localPath));
            if (string.IsNullOrEmpty(remotePath)) throw new ArgumentNullException(nameof(remotePath));
            if (Directory.Exists(localPath))
            {
                if (!localPath.EndsWith("/") && !localPath.EndsWith("\\")) localPath += "/";
                if (!remotePath.EndsWith("/")) remotePath += "/";
                var entries = Directory.GetFiles(localPath)
                    .Union(Directory.GetDirectories(localPath).Select(x=>x+"/"));
                var directoryName = Path.GetFileName(Path.GetDirectoryName(localPath));
                var remotePathAppended = ($"{remotePath}/{directoryName}/").Replace("//", "/");
                try
                {
                    foreach (var entry in entries)
                    {
                        var remoteFilePath = remotePathAppended + Path.GetFileName(entry);
                        this.UploadInternal(entry, remoteFilePath, preProcess, true);
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (keepConnectionOpen != true)
                        try
                        {
                            this.Disconnect();
                        }
                        catch { }
                }
                return;
            }
            if (!File.Exists(localPath)) throw new Exception("Unable to access file '" + localPath + "'");
            if (remotePath.EndsWith("/")) remotePath += Path.GetFileName(localPath);
            using (var file = File.OpenRead(localPath))
            {
                this.UploadInternal(file, remotePath, preProcess, keepConnectionOpen);
            }

        }
        /// <summary>
        /// Uploads data from a stream to a remote file.
        /// </summary>
        /// <param name="input">Input stream to read from</param>
        /// <param name="remotePath">Remote file path to upload to</param>
        /// <param name="preProcess">Optional function to pre-process the file before upload. Note: If provided, the stream's position will be advanced to the end.</param>
        /// <param name="closeStream">If true, closes the stream after upload. If false, leaves it open. If null, uses the AutoCloseStreams property value.</param>
        public void Upload(Stream input, string remotePath, Func<string, string> preProcess = null, bool? closeStream = null) 
            => this.UploadInternal(input, remotePath, preProcess, this.KeepConnectionOpen, closeStream);
        private void UploadInternal(Stream input, string remotePath, Func<string, string> preProcess = null, bool? keepConnectionOpen = null, bool? closeStream = null)
        {
            if (keepConnectionOpen is null) keepConnectionOpen = this.KeepConnectionOpen;
            if (closeStream is null) closeStream = this.AutoCloseStreams ?? GlobalAutoCloseStreams;
            
            string tempPath = null;
            string backupTempPath = null;

            try
            {
                if (input == null) throw new ArgumentNullException(nameof(input));
                this.AutoConnect();
                var folders = Regex.Matches(remotePath, SFtpClient.FolderSplitterRegex)
                    .Cast<Match>().Select(x => x.Value)
                    .Select(x => x.Replace("\\", "/")).ToList();
                string folder_to_create = "";
                foreach (var folder in folders)
                {
                    folder_to_create += folder;
                    if (!this.Client.Exists(folder_to_create))
                    {
                        this.Client.CreateDirectory(folder_to_create);
                    }
                }
                if (preProcess != null)
                {
                    using (var temp = File.OpenWrite(tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp")))
                        input.CopyTo(temp);
                    // Note: input stream position is now at the end after CopyTo
                    // The caller is responsible for closing the input stream if needed
                    tempPath = preProcess(backupTempPath = tempPath);
                    using (var temp = File.OpenRead(tempPath))
                        this.Client.UploadFile(temp, remotePath, true);
                }
                else
                    this.Client.UploadFile(input, remotePath, true);
            }
            catch { throw; }
            finally
            {
                try
                {
                    if (closeStream == true)
                        input?.Dispose();
                }
                catch { }
                try
                {
                    if (keepConnectionOpen != true)
                        this.Disconnect();
                }
                catch { }
                try
                {
                    if (tempPath != null)
                        File.Delete(tempPath);
                }
                catch { }
                try
                {
                    if (backupTempPath != null)
                        File.Delete(backupTempPath);
                }
                catch { }

            }
        }
        #endregion

        #region get files
        public List<SFtpFileInfo> ListFiles(string remotePath) => this.ListFilesInternal(remotePath, this.KeepConnectionOpen);
        private List<SFtpFileInfo> ListFilesInternal(string remotePath = null, bool? keepConnectionOpen = null)
        {
            if (keepConnectionOpen is null) keepConnectionOpen = this.KeepConnectionOpen;
            try
            {
                if (string.IsNullOrWhiteSpace(remotePath)) remotePath= "";
                this.AutoConnect();
                List<SFtpFileInfo> list = new List<SFtpFileInfo>();
                foreach (var fileInfo in this.Client.ListDirectory(remotePath)
                    .Where(x=>!(x.Name?.Equals(".") == true
			            || x.Name?.Equals("..") == true))
                )
                {
                    list.Add(new SFtpFileInfo()
                    {
                        Name = fileInfo.Name,
                        FullPath = fileInfo.FullName,
                        IsDirectory = fileInfo.IsDirectory,
                        LastModified = fileInfo.LastWriteTime,
                    });
                }
                return list;
            }
            catch { throw; }
            finally
            {
                try
                {
                    if (keepConnectionOpen != true)
                        this.Disconnect();
                }
                catch { }
            }

        }

        public SFtpFileInfo GetFileInfo(string remotePath) => this.GetFileInfoInternal(remotePath, this.KeepConnectionOpen);
        private SFtpFileInfo GetFileInfoInternal(string remotePath, bool? keepConnectionOpen = null)
        {
            if (keepConnectionOpen is null) keepConnectionOpen = this.KeepConnectionOpen;
            try
            {
                this.AutoConnect();
                var fileInfo = this.Client.Get(remotePath);
                return new SFtpFileInfo()
                {
                    Name = fileInfo.Name,
                    FullPath = fileInfo.FullName,
                    IsDirectory = fileInfo.IsDirectory,
                    LastModified = fileInfo.LastWriteTime,
                };
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    if (keepConnectionOpen != true)
                        this.Disconnect();
                }
                catch { }
            }
        }

        #endregion

        #region exists

        public bool Exists(string remotePath) => this.ExistsInternal(remotePath, this.KeepConnectionOpen);
        private bool ExistsInternal(string remotePath, bool? keepConnectionOpen = null)
        {
            if (this.GetFileInfoInternal(remotePath, keepConnectionOpen) == null) return false;
            return true;
        }
        #endregion


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    try
                    {
                        if (this.c != null)
                        {
                            this.Disconnect();
                            this.c.Dispose();
                            this.c = null;
                        }
                    }
                    catch { }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}






