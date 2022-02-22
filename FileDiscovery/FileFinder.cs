using SMBeagle.Output;
using SMBeagle.ShareDiscovery;
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

        public FileFinder(List<Share> shares, bool enumerateLocalDrives = true, bool getPermissionsForSingleFileInDir = true, bool enumerateAcls = true, bool quiet = false, bool verbose = false, bool crossPlatform = false)
        {
            pClientContext = IntPtr.Zero;
            if (! crossPlatform)
            {
                #pragma warning disable CA1416
                pClientContext = WindowsPermissionHelper.GetpClientContext();
                if (enumerateAcls & pClientContext == IntPtr.Zero & !quiet)
                {
                    OutputHelper.WriteLine("!! Error querying user context.  Failing back to a slower ACL identifier.  ", 1);
                    OutputHelper.WriteLine("    We can also no longer check  if a file is deletable", 1);
                    if (!getPermissionsForSingleFileInDir)
                        OutputHelper.WriteLine("    It is advisable to set the fast flag and only check the ACLs of one file per directory", 1);
                }
                #pragma warning restore CA1416
            }

            foreach (Share share in shares) //TODO: dedup share by host and name
            {
                _directories.Add(new Directory(path: "", share:share) { DirectoryType = Enums.DirectoryTypeEnum.SMB });
            }

            /* TODO: Reimplement in future
            if (enumerateLocalDrives)
                _directories.AddRange(GetLocalDriveDirectories());
            */

            if (!quiet)
                OutputHelper.WriteLine($"6a. Enumerating all subdirectories for known paths");
            foreach (Directory dir in _directories)
            {
                if (verbose)
                    OutputHelper.WriteLine($"Enumerating all subdirectories for '{dir.UNCPath}'",1);
                dir.FindDirectoriesRecursively(crossPlatform: crossPlatform);
            }

            if(!quiet)
                OutputHelper.WriteLine($"6b. Splitting large directories to optimise caching and to batch output");

            SplitLargeDirectories();

            if (!quiet)
                OutputHelper.WriteLine($"6c. Enumerating files in directories");

            foreach (Directory dir in _directories)
            {
                OutputHelper.WriteLine($"\renumerating files in '{dir.UNCPath}'                                          ", 1, false);
                // TODO: pass in the ignored extensions from the commandline
                dir.FindFilesRecursively(crossPlatform: crossPlatform, extensionsToIgnore: new List<string>() { ".dll",".manifest",".cat" });
                if (verbose)
                    OutputHelper.WriteLine($"\rFound {dir.ChildDirectories.Count} child directories and {dir.RecursiveFiles.Count} files in '{dir.UNCPath}'",2);
                
                foreach (File file in dir.RecursiveFiles)
                {
                    if (FilesSentForOutput.Add($"{dir.Share.uncPath}{file.FullName}".ToLower())) // returns True if not already present
                    {
                        // Cache fullnames and dont send a dupe
                        if (enumerateAcls)
                            FetchFilePermission(file, crossPlatform, getPermissionsForSingleFileInDir);

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

        //TODO: Reimplement at some point
        /*private List<Directory> GetLocalDriveDirectories()
        {
            // Create dummy sahre
            Share dummyShare = new Share(new HostDiscovery.Host("localhost"), "", Enums.ShareTypeEnum.DISK);
            return DriveInfo
                .GetDrives()
                .Where(drive => drive.IsReady)
                .Select(drive => new Directory(drive.Name, share: dummyShare) { DirectoryType = DriveInfoTypeToDirectoryTypeEnum(drive.DriveType) })
                .ToList();
        }*/

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

        private void FetchFilePermission(File file, bool crossPlatform, bool useCache = true)
        {
            if (useCache && CacheACL.Keys.Contains(file.ParentDirectory.Path)) // If we should use cache and cache has a hit
                file.SetPermissionsFromACL(CacheACL[file.ParentDirectory.Path]);
            else
            {
                ACL permissions;
                if (!crossPlatform)
                #pragma warning disable CA1416
                {
                    if (pClientContext != IntPtr.Zero)
                        permissions = WindowsPermissionHelper.ResolvePermissions(file.FullName, pClientContext);

                    else
                        permissions = WindowsPermissionHelper.ResolvePermissionsSlow(file.FullName);
                    
                }
                #pragma warning restore CA1416
                else
                {
                    permissions = CrossPlatformPermissionHelper.ResolvePermissions(file);
                }
                file.SetPermissionsFromACL(permissions);

            if (useCache)
                CacheACL[file.ParentDirectory.Path] = permissions;
            }
        }
    }
}
