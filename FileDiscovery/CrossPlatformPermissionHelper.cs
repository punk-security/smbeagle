using SMBLibrary.Client;
using SMBLibrary;
using System;

namespace SMBeagle.FileDiscovery
{
    class CrossPlatformPermissionHelper
    {
        private static bool CheckAccessMask(ISMBFileStore fileStore, string filePath, AccessMask accessMask)
        {
            if (fileStore is SMB1FileStore)
            {
                filePath = @"\\" + filePath;
            }
            object handle;
            NTStatus result = fileStore.CreateFile(out handle, out _, filePath, accessMask, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
            if (result == NTStatus.STATUS_SUCCESS)
                fileStore.CloseFile(handle);
            return result == NTStatus.STATUS_SUCCESS;
        }
        public static ACL ResolvePermissions(File file)
        {
            ACL acl = new();
            if (file.ParentDirectory.Share == null)
            {
                Console.WriteLine("ERROR: File does not have a parent share");
                Environment.Exit(1);
            }
            NTStatus status;
            //TODO: optimise storing filestore somewhere
            ISMBFileStore fileStore = file.ParentDirectory.Share.Host.Client.TreeConnect(file.ParentDirectory.Share.Name, out status);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                Console.WriteLine("ERROR: Could not connect to share");
            }
            string filePath = file.FullName;
            acl.Readable = CheckAccessMask(fileStore, filePath, AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE);
            acl.Writeable = CheckAccessMask(fileStore, filePath, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE);
            acl.Deletable = CheckAccessMask(fileStore, filePath, AccessMask.DELETE | AccessMask.SYNCHRONIZE);
            fileStore.Disconnect();
            return acl;
        }
    }

}
