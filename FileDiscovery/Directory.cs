using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMBeagle.FileDiscovery
{
    class Directory
    {
        //DirectoryInfo DirInfo { get; set; }
        public string Path { get; set; }
        //todo: replace Base and Type with direct copy from parent then drop the ref
        public Directory? Parent { get; set; } = null;
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
        public Directory(string path)
        {
            Path = path;
        }
        public void FindFiles(string pattern = "*.*", List<string> extensionsToIgnore = null)
        {
            try
            {
                FileInfo[] files = new DirectoryInfo(Path).GetFiles(pattern);
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

        public void Clear()
        {
            Files.Clear();
            ChildDirectories.Clear();
            GC.Collect();
        }

        private void FindDirectories()
        {
            try
            {
                DirectoryInfo[] subDirs = new DirectoryInfo(Path).GetDirectories();
                foreach (DirectoryInfo di in subDirs)
                    ChildDirectories.Add(new Directory(path: di.FullName) { Parent = this});
            }
            catch { }
        }
        public void FindDirectoriesRecursively()
        {
            FindDirectories();
            foreach (Directory dir in ChildDirectories)
            {
                dir.FindDirectoriesRecursively();
            }
        }

        public void FindFilesRecursively(string pattern = "*.*", List<string> extensionsToIgnore = null)
        {
            FindFiles(pattern, extensionsToIgnore);
            foreach (Directory dir in RecursiveChildDirectories)
            {
                dir.FindFilesRecursively(pattern, extensionsToIgnore);
            }
        }

        public void EnumerateFullTree()
        {
            FindDirectoriesRecursively();
            FindFilesRecursively();
        }

    }
}
