using SMBeagle.Output;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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

        public FileFinder(List<string> paths, bool enumerateLocalDrives = true, bool getPermissionsForSingleFileInDir = true, string username="", bool enumerateAcls = true, bool quiet = false, bool verbose = false)
        {

            pClientContext = IntPtr.Zero;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                #pragma warning disable CA1416
                pClientContext = WindowsPermissionHelper.GetpClientContext(username);
                if (enumerateAcls & pClientContext == IntPtr.Zero & !quiet)
                {
                    OutputHelper.WriteLine("!! Error querying user context.  Failing back to a slower ACL identifier.  ", 1);
                    OutputHelper.WriteLine("    We can also no longer check  if a file is deletable", 1);
                    if (!getPermissionsForSingleFileInDir)
                        OutputHelper.WriteLine("    It is advisable to set the fast flag and only check the ACLs of one file per directory", 1);
                }
                #pragma warning restore CA1416
            }

            paths = new HashSet<string>(paths.ConvertAll(d => d.ToLower())).ToList();

            foreach (string path in paths)
            {
                _directories.Add(new Directory(path) { DirectoryType = Enums.DirectoryTypeEnum.SMB });
            }

            if (enumerateLocalDrives)
                _directories.AddRange(GetLocalDriveDirectories());

            if (!quiet)
                OutputHelper.WriteLine($"6a. Enumerating all subdirectories for known paths");
            foreach (Directory dir in _directories)
            {
                if (verbose)
                    OutputHelper.WriteLine($"Enumerating all subdirectories for '{dir.Path}'",1);
                dir.FindDirectoriesRecursively();
            }

            if(!quiet)
                OutputHelper.WriteLine($"6b. Splitting large directories to optimise caching and to batch output");

            SplitLargeDirectories();

            if (!quiet)
                OutputHelper.WriteLine($"6c. Enumerating directories");

            foreach (Directory dir in _directories)
            {
                OutputHelper.WriteLine($"\renumerating '{dir.Path}'                                          ", 1, false);
                // TODO: pass in the ignored extensions from the commandline
                dir.FindFilesRecursively(extensionsToIgnore: new List<string>() { ".dll",".manifest",".cat" });
                if (verbose)
                    OutputHelper.WriteLine($"\rFound {dir.ChildDirectories.Count} child directories and {dir.RecursiveFiles.Count} files in '{dir.Path}'",2);
                
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
            OutputHelper.WriteLine($"\r  file enumeration complete, {FilesSentForOutput.Count} files identified                ");
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
                    #pragma warning disable CA1416
                    permissions = WindowsPermissionHelper.ResolvePermissions(file.FullName, pClientContext);
                    #pragma warning restore CA1416
                else
                    permissions = PermissionHelper.ResolvePermissions(file.FullName);

                file.SetPermissionsFromACL(permissions);

                if (useCache)
                    CacheACL[file.ParentDirectory.Path] = permissions;
            }
        }
    }
}
