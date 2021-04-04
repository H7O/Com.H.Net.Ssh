using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Com.H.Net.Ssh
{
    /// <summary>
    /// Requires SSH.NET API Renci.SshNet.dll (https://github.com/sshnet/SSH.NET). 
    /// local location Repo/20xx/3rd/SSH.NET/src/Renci.SshNet/bin/Release
    /// </summary>
    public class SFtpClient : IDisposable
    {
        private Renci.SshNet.SftpClient c;
        public Renci.SshNet.SftpClient Client
        {
            get
            {
                if (c == null) c = new Renci.SshNet.SftpClient(
                    this.ServerAddress, 
                    this.Port, 
                    this.UId, 
                    this.Pwd);
                
                return c;
            }
        }
        private void Connect()
        {
            if (this.Client.IsConnected) return;
            this.Client.Connect();
        }

        private void Disconnect()
        {
            if (!this.Client.IsConnected) return;
            this.Client.Disconnect();
        }
        public string UId { get; set; }
        public string Pwd { get; set; }
        public string ServerAddress { get; set; }
        public int Port { get; set; } = 22;

        private const string FolderSplitterRegex = @".+?[/|\\]";
        public SFtpClient(string serverAddress, string uid, string pwd) =>
            (this.ServerAddress, this.UId, this.Pwd)
            = (serverAddress, uid, pwd);

        public SFtpClient(string serverAddress, int port, string uid, string pwd) =>
            (this.ServerAddress, this.Port, this.UId, this.Pwd)
            = (serverAddress, port, uid, pwd);


        public bool Exist(string remotePath)
        {
            try
            {
                this.Connect();
                return this.Client.Exists(remotePath);
            }
            catch { throw; }
            finally
            {
                try
                {
                    this.Disconnect();
                }
                catch { }
            }
        }
        public void Download(string remotePath, string localPath)
        {
            if (string.IsNullOrWhiteSpace(remotePath)) throw new Exception("empty remote_path");
            if (string.IsNullOrWhiteSpace(localPath)) throw new Exception("empty local_path");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localPath));
            }
            catch { }

            using var file = File.OpenWrite(localPath);
            this.Download(remotePath, file);
            file.Flush();
            file.Close();
        }
        public void Download(string remotePath, Stream output)
        {
            try
            {
                this.Connect();
                this.Client.DownloadFile(remotePath, output);
            }
            catch { throw; }
            finally
            {
                try
                {
                    this.Disconnect();
                }
                catch { }
            }
        }


        public string DownloadContent(string remoteFilePath, 
            Encoding encoding = null,
            Func<string, string> postProcess = null
            )
        {
            string tempPath = null;
            string tempPathBackup = null;
            try
            {
                using (var f = File.OpenWrite(tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp")))
                    this.Download(remoteFilePath, f);
                if (postProcess!=null)
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
                    if (tempPath!=null)
                        File.Delete(tempPath);
                }
                catch { }
                try
                {
                    if(tempPathBackup!=null)
                        File.Delete(tempPathBackup);
                }
                catch { }
            }
            
        }


        public void Delete(string remotePath)
        {
            try
            {
                this.Connect();
                this.Client.Delete(remotePath);
            }
            catch { throw; }
            finally
            {
                try
                {
                    this.Disconnect();
                }
                catch { }
            }
        }


        public void Upload(string localPath, string remotePath, Func<string, string> preProcess = null)
        {
            if (string.IsNullOrEmpty(remotePath)) throw new ArgumentNullException(nameof(remotePath));
            if (!File.Exists(localPath)) throw new Exception("Unable to access file '" + localPath + "'");
            using var file = File.OpenRead(localPath);
            this.Upload(file, remotePath, preProcess);
            
        }

        public void Upload(Stream input, string remotePath, Func<string, string> preProcess = null)
        {
            string tempPath = null;
            string backupTempPath = null;

            try
            {
                if (input == null) throw new ArgumentNullException(nameof(input));
                this.Connect();
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
                if (preProcess!=null)
                {
                    using (var temp = File.OpenWrite(tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp")))
                        input.CopyTo(temp);
                    input.Close();
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
                    this.Disconnect();
                }
                catch { }
                try
                {
                    if (tempPath!=null)
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

        public List<SFtpFileInfo> GetFiles(string remotePath = null)
        {
            try
            {
                remotePath ??= "";
                this.Connect();
                List<SFtpFileInfo> list = new List<SFtpFileInfo>();
                foreach (var fileInfo in this.Client.ListDirectory(remotePath))
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
                    this.Disconnect();
                }
                catch { }
            }

        }

        public bool Exists(string remotePath)
        {
            if (this.GetFile(remotePath) == null) return false;
            return true;
        }

        public SFtpFileInfo GetFile(string remotePath)
        {
            try
            {
                this.Connect();
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
                    this.Disconnect();
                }
                catch { }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        if (this.c == null) return;
                        this.Disconnect();
                    }
                    catch { }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SFtpClient() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
