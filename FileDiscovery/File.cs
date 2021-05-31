using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SMBeagle.FileDiscovery
{
    class File
    {
        public Directory ParentDirectory { get; set; }
        public string FullName { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public bool Readable { get; set; }
        public bool Writeable { get; set; }
        public bool Deletable { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }

        public File(string name, string fullName, string extension, DateTime creationTime, DateTime lastWriteTime, Directory parentDirectory)
        {
            Name = name;
            Extension = extension;
            CreationTime = creationTime;
            LastWriteTime = lastWriteTime;
            ParentDirectory = parentDirectory;
            FullName = fullName;
            /*try
            {
                AuthorizationRuleCollection rules = acl.GetAccessRules(true, true, typeof(NTAccount));
                foreach (FileSystemAccessRule rule in rules)
                {
                    //todo: ignore system and administrators // presume admins can access everything
                    //todo: how we do check if user is local admin on target?
                    if (rule.AccessControlType == AccessControlType.Deny)
                        continue; //todo: check for a deny rule?  how do we do this without massive work
                    if ((rule.FileSystemRights & FileSystemRights.WriteData) > 0)
                        AccountsWhoCanWrite.Add(rule.IdentityReference.Value);
                    if ((rule.FileSystemRights & FileSystemRights.Delete) > 0)
                        AccountsWhoCanDelete.Add(rule.IdentityReference.Value);
                    if ((rule.FileSystemRights & FileSystemRights.ReadData) > 0)
                        AccountsWhoCanRead.Add(rule.IdentityReference.Value);

                };
            }
            catch { };*/
        }

        public void SetPermissions(bool read, bool write, bool delete)
        {
            Readable = read;
            Writeable = write;
            Deletable = delete;
        }

        public void SetPermissionsFromACL(ACL acl)
        {
            Readable = acl.Readable;
            Writeable = acl.Writeable;
            Deletable = acl.Deletable;
        }
    }
}
