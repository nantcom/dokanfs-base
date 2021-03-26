using DokanNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace NC.DokanFS
{
    /// <summary>
    /// Base ReadOnly Disk which throws UnauthorizedAccessException for all writing operations
    /// </summary>
    public abstract class ReadOnlyDiskBase<T> : IDokanDisk
        where T : ReadOnlyFileContextBase, new()
    {
        /// <summary>
        /// Disk size to show
        /// </summary>
        public abstract long TotalSize { get; }

        public abstract void CleanUp();

        public virtual IDokanFileContext CreateFileContext(string path, FileMode mode, System.IO.FileAccess access, FileShare share = FileShare.None, FileOptions options = FileOptions.None)
        {
            if (mode == FileMode.Append ||
                mode == FileMode.Truncate ||
                mode == FileMode.OpenOrCreate ||
                mode == FileMode.Create ||
                mode == FileMode.CreateNew)
            {
                throw new UnauthorizedAccessException();
            }

            if (access == System.IO.FileAccess.Write ||
                access == System.IO.FileAccess.ReadWrite)
            {
                throw new UnauthorizedAccessException();
            }

            return new T();
        }

        public abstract bool GetFileInfo(string path, out FileInformation fi);

        public abstract string GetPath(string osPath);

        public abstract bool IsDirectory(string path);

        public abstract bool DirectoryExists(string path);

        public abstract bool FileExists(string path);

        public abstract IList<FileInformation> FindFiles(string directory, string searchPattern);

        #region Writing Operations

        public void CreateDirectory(string path)
        {
            throw new UnauthorizedAccessException();
        }

        public void DeleteDirectory(string path)
        {
            throw new UnauthorizedAccessException();
        }

        public void DeleteFile(string path)
        {
            throw new UnauthorizedAccessException();
        }

        public bool DirectoryCanBeDeleted(string path)
        {
            return false;
        }

        public void GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes)
        {
            freeBytesAvailable = 0;
            totalNumberOfFreeBytes = 0;
            totalNumberOfBytes = this.TotalSize;
        }

        public bool IsDirectoryEmpty(string path)
        {
            return false;
        }

        public void MoveDirectory(string oldPath, string newPath)
        {
            throw new UnauthorizedAccessException();
        }

        public void MoveFile(string oldPath, string newPath)
        {
            throw new UnauthorizedAccessException();
        }

        public void SetFileAttribute(string path, FileAttributes attr)
        {
            throw new UnauthorizedAccessException();
        }

        public void SetFileTime(string path, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime)
        {
            throw new UnauthorizedAccessException();
        }

        public IDokanFile Touch(string path, FileAttributes attributes)
        {
            throw new UnauthorizedAccessException();
        }

        #endregion
    }
}
