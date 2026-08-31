using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.H.Net.Ssh
{
    public class SFtpFileInfo
    {
        public string Name;
        public string ParentFolderPath;
        public string FullPath;
        public DateTime? LastModified;
        public string Flags;
        public string Owner;
        public string Group;
        public bool IsDirectory;
        public long Size;
    }
}
