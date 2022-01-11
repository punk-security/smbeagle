using System.IO;

namespace SMBeagle.FileDiscovery
{
    class PermissionHelper
    {
        public static ACL ResolvePermissions(string path)
        {
            ACL acl = new();
            try
            {
                new FileStream(path, FileMode.Open, FileAccess.Read).Dispose();
                acl.Readable = true;
            }
            catch { }
            try
            {
                new FileStream(path, FileMode.Open, FileAccess.Write).Dispose();
                acl.Writeable = true;
            }
            catch { }
            return acl;
        }
    }
}