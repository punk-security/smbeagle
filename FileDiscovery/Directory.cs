using SMBeagle.ShareDiscovery;
using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.IO;

namespace SMBeagle.FileDiscovery
{
    class Directory
    {
        public Share Share { get; set; }
        public string Path { get; set; }
        public string UNCPath
        {
            get
            {
                // Windows enum needs UNC Paths as Path but Cross-platform doesnt.
                if (Path.StartsWith(@"\\"))
                    return Path;
                else
                    return $"{Share.uncPath}{Path}";
            }
        }
        //todo: replace Base and Type with direct copy from parent then drop the ref
        #nullable enable
        public Directory? Parent { get; set; } = null;
        #nullable disable
        public Directory Base { get
            {
                if (Parent == null)
                    return this;
                else
                    return Parent.Base;
            }
        }
        public Enums.DirectoryTypeEnum DirectoryType { get; set; } = Enums.DirectoryTypeEnum.UNKNOWN;
        public List<File> RecursiveFiles
        {
            get
            {
                List<File> ret = new List<File>();
                ret.AddRange(Files);
                foreach (Directory dir in ChildDirectories)
                {
                    ret.AddRange(dir.RecursiveFiles);
                }
                return ret;
            }
        }

        public List<Directory> RecursiveChildDirectories
        {
            get
            {
                List<Directory> ret = new List<Directory>();
                ret.AddRange(ChildDirectories);
                foreach (Directory dir in ChildDirectories)
                {
                    ret.AddRange(dir.ChildDirectories);
                }
                return ret;
            }
        }

        public List<File> Files { get; private set; } = new List<File>();
        public List<Directory> ChildDirectories { get; private set; } = new List<Directory>();
        public Directory(string path, Share share)
        {
            Share = share;
            Path = path;
        }
        public void FindFilesWindows(List<string> extensionsToIgnore = null)
        {
            try
            {
                FileInfo[] files = new DirectoryInfo(Path).GetFiles("*.*");
                foreach (FileInfo file in files)
                {
                    if (extensionsToIgnore.Contains(file.Extension.ToLower()))
                        continue;
                    Files.Add(
                        new File(
                            parentDirectory: this,
                            name: file.Name,
                            fullName: file.FullName,
                            extension: file.Extension,
                            creationTime: file.CreationTime,
                            lastWriteTime: file.LastWriteTime
                        )
                    );
                }
            }
            catch  {            }
        }
        public void FindFilesCrossPlatform(List<string> extensionsToIgnore = null)
        {
            try
            {
                NTStatus status;
                ISMBFileStore fileStore = Share.Host.Client.TreeConnect(Share.Name, out status);
                if (status == NTStatus.STATUS_SUCCESS)
                {
                    object directoryHandle;
                    FileStatus fileStatus;
                    status = fileStore.CreateFile(out directoryHandle, out fileStatus, Path, AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
                    if (status == NTStatus.STATUS_SUCCESS)
                    {
                        List<QueryDirectoryFileInformation> fileList;
                        //TODO: can we filter on just files
                        status = fileStore.QueryDirectory(out fileList, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);
                        foreach (QueryDirectoryFileInformation f in fileList)
                        {
                            if (f.FileInformationClass == FileInformationClass.FileDirectoryInformation)
                            {
                                FileDirectoryInformation d = (FileDirectoryInformation)f;
                                if (d.FileAttributes != SMBLibrary.FileAttributes.Directory)
                                {
                                    string extension = d.FileName.Substring(d.FileName.LastIndexOf('.') + 1);
                                    string path;
                                    if (Path == "")
                                        path = d.FileName;
                                    else
                                        path = $"{Path}\\{d.FileName}";
                                    if (extensionsToIgnore.Contains(extension.ToLower()))
                                        continue;
                                    Files.Add(
                                        new File(
                                            parentDirectory: this,
                                            name: d.FileName,
                                            fullName: path,
                                            extension: extension,
                                            creationTime: d.CreationTime,
                                            lastWriteTime: d.LastWriteTime
                                        )
                                    );
                                }
                            }
                        }
                        status = fileStore.CloseFile(directoryHandle);
                    }
                    status = fileStore.Disconnect();
                }
            }
            catch { }
        }
        public void Clear()
        {
            Files.Clear();
            ChildDirectories.Clear();
        }

        private void FindDirectoriesWindows()
        {
            try
            {
                DirectoryInfo[] subDirs = new DirectoryInfo(UNCPath).GetDirectories();
                foreach (DirectoryInfo di in subDirs)
                    ChildDirectories.Add(new Directory(path: di.FullName, share: Share) { Parent = this});
            }
            catch { }
        }
        private void FindDirectoriesCrossPlatform()
        {
            try
            {
                NTStatus status;
                ISMBFileStore fileStore = Share.Host.Client.TreeConnect(Share.Name, out status);
                if (status == NTStatus.STATUS_SUCCESS)
                {
                    object directoryHandle;
                    FileStatus fileStatus;
                    status = fileStore.CreateFile(out directoryHandle, out fileStatus, Path, AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
                    if (status == NTStatus.STATUS_SUCCESS)
                    {
                        List<QueryDirectoryFileInformation> fileList;
                        //TODO: can we filter on just files
                        status = fileStore.QueryDirectory(out fileList, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);
                        foreach (QueryDirectoryFileInformation f in fileList)
                        {
                            if (f.FileInformationClass == FileInformationClass.FileDirectoryInformation)
                            {
                                FileDirectoryInformation d = (FileDirectoryInformation) f;
                                if (d.FileAttributes == SMBLibrary.FileAttributes.Directory && d.FileName != "." && d.FileName != "..")
                                {
                                    string path = "";
                                    if (Path != "")
                                        path += $"{Path}\\";
                                    path += d.FileName;
                                    ChildDirectories.Add(new Directory(path: path, share: Share) { Parent = this });
                                }
                            }
                        }
                        status = fileStore.CloseFile(directoryHandle);
                    }
                    status = fileStore.Disconnect();
                } 
            }
            catch { }
        }
        public void FindDirectoriesRecursively(bool crossPlatform)
        {
            if (crossPlatform)
                FindDirectoriesCrossPlatform();
            else
                FindDirectoriesWindows();
            foreach (Directory dir in ChildDirectories)
            {
                dir.FindDirectoriesRecursively(crossPlatform);
            }
        }

        public void FindFilesRecursively(bool crossPlatform, List<string> extensionsToIgnore = null)
        {
            if (crossPlatform)
                FindFilesCrossPlatform(extensionsToIgnore);
            else
                FindFilesWindows(extensionsToIgnore);
            foreach (Directory dir in RecursiveChildDirectories)
            {
                dir.FindFilesRecursively(crossPlatform, extensionsToIgnore);
            }
        }

    }
}
