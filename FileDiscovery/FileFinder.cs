using SMBeagle.Output;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SMBeagle.FileDiscovery
{
    class FileFinder
    {
        Dictionary<string, ACL> CacheACL { get; set; } = new();
        IntPtr pClientContext { get; set; }
        HashSet<string> FilesSentForOutput { get; set; } = new();

        List<Directory> _directories { get; set; } = new();
        public List<Directory> Directories
        {
            get
            {
                List<Directory> 
                    ret = new ();

                ret.AddRange(_directories);

                foreach (Directory dir in _directories)
                {
                    ret.AddRange(dir.RecursiveChildDirectories);
                }

                return ret;
            }
        }

        public List<File> Files
        {
            get
            {
                return Directories
                        .SelectMany(dir => dir.RecursiveFiles)
                        .ToList();
            }
        }

        public FileFinder(List<string> paths, bool enumerateLocalDrives = true, bool getPermissionsForSingleFileInDir = true, string username="", bool enumerateAcls = true)
        {
            pClientContext = PermissionHelper.GetpClientContext(username);
            if (enumerateAcls & pClientContext == IntPtr.Zero)
            {
                Console.WriteLine("Error querying user context.  Failing back to a slower ACL identifier.  We can also no longer check  if a file is deletable");
                if (! getPermissionsForSingleFileInDir)
                    Console.WriteLine("It is advisable to set the fast flag and only check the ACLs of one file per directory");
            }
            paths = new HashSet<string>(paths.ConvertAll(d => d.ToLower())).ToList();

            foreach (string path in paths)
            {
                _directories.Add(new Directory(path) { DirectoryType = Enums.DirectoryTypeEnum.SMB });
            }

            if (enumerateLocalDrives)
                _directories.AddRange(GetLocalDriveDirectories());

            foreach (Directory dir in _directories)
            {
                Console.WriteLine("Finding dirs for {0}", dir.Path);
                dir.FindDirectoriesRecursively();
            }

            Console.WriteLine("Splitting dirs");

            SplitLargeDirectories();

            foreach (Directory dir in _directories)
            {
                // TODO: pass in the ignored extensions from the commandline
                dir.FindFilesRecursively(extensionsToIgnore: new List<string>() { ".dll",".manifest",".cat" });

                Console.WriteLine("Found {0} child directories and {1} files in '{2}'.{3}", dir.ChildDirectories.Count , dir.RecursiveFiles.Count, dir.Path, enumerateAcls ? " Fetching permissions..." : "");
                
                foreach (File file in dir.RecursiveFiles)
                {
                    if (FilesSentForOutput.Add(file.FullName.ToLower())) // returns True if not already present
                    {
                        // Cache fullnames and dont send a dupe
                        if (enumerateAcls)
                            FetchFilePermission(file, getPermissionsForSingleFileInDir);

                        OutputHelper.AddPayload(new Output.FileOutput(file), Enums.OutputtersEnum.File);
                    }
                }

                dir.Clear();
                CacheACL.Clear(); // Clear Cached ACLs otherwise it grows and grows
            }
        }

        private Enums.DirectoryTypeEnum DriveInfoTypeToDirectoryTypeEnum(DriveType type)
        {
            return type switch
            {
                DriveType.Fixed => Enums.DirectoryTypeEnum.LOCAL_FIXED,
                DriveType.CDRom => Enums.DirectoryTypeEnum.LOCAL_CDROM,
                DriveType.Network => Enums.DirectoryTypeEnum.LOCAL_NETWORK,
                DriveType.Removable => Enums.DirectoryTypeEnum.LOCAL_REMOVEABLE,
                _ => Enums.DirectoryTypeEnum.UNKNOWN
            };
        }

        private List<Directory> GetLocalDriveDirectories()
        {
            return DriveInfo
                .GetDrives()
                .Where(drive => drive.IsReady)
                .Select(drive => new Directory(drive.Name) { DirectoryType = DriveInfoTypeToDirectoryTypeEnum(drive.DriveType) })
                .ToList();
        }

        private void SplitLargeDirectories(int maxChildCount = 20)
        {
            List<Directory> 
                oversizedDirectories = _directories.Where(item => item.RecursiveChildDirectories.Count > maxChildCount).ToList();

            while (oversizedDirectories.Count > 0)
            {
                foreach (Directory dir in oversizedDirectories)
                {
                    _directories.Remove(dir);
                    _directories.AddRange(dir.ChildDirectories);
                }

                oversizedDirectories = _directories.Where(item => item.RecursiveChildDirectories.Count > maxChildCount).ToList();
            }
        }

        private void FetchFilePermission(File file, bool useCache = true)
        {
            if (useCache && CacheACL.Keys.Contains(file.ParentDirectory.Path)) // If we should use cache and cache has a hit
                file.SetPermissionsFromACL(CacheACL[file.ParentDirectory.Path]);
            else
            {
                ACL permissions;
                if (pClientContext != IntPtr.Zero)
                    permissions = PermissionHelper.ResolvePermissionsViaWinApi(file.FullName, pClientContext);
                else
                    permissions = PermissionHelper.ResolvePermissionsViaFileStream(file.FullName);

                file.SetPermissionsFromACL(permissions);

                if (useCache)
                    CacheACL[file.ParentDirectory.Path] = permissions;
            }
        }
    }
}
