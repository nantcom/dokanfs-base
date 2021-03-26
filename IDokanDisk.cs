using DokanNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.DokanFS
{
    public interface IDokanDisk
    {
        /// <summary>
        /// Disk Unique Identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Volume Label (shown in Explorer)
        /// </summary>
        string VolumeLabel { get; }

        /// <summary>
        /// Name of the file system to show in explorer
        /// </summary>
        string FileSystemName { get; }

        /// <summary>
        /// Gets the file system features
        /// </summary>
        FileSystemFeatures FileSystemFeatures { get; }

        /// <summary>
        /// Max Path Length, should be 256
        /// </summary>
        uint MaximumComponentLength { get; }

        /// <summary>
        /// Gets this system's path from given OS Path
        /// </summary>
        /// <param name="osPath">Path as specified by OS</param>
        /// <returns>osPath converted to current systen's path format - which will be used as parameters in all other functions that accept path</returns>
        string GetPath(string osPath);

        /// <summary>
        /// Create a new File Context from given parameters
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mode"></param>
        /// <param name="access"></param>
        /// <param name="share"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        IDokanFileContext CreateFileContext(string path, FileMode mode, System.IO.FileAccess access, FileShare share = FileShare.None, FileOptions options = FileOptions.None);

        /// <summary>
        /// Create a directory if it does not exists
        /// </summary>
        /// <param name="path"></param>
        void CreateDirectory(string path);

        /// <summary>
        /// Whether given path is a directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns>false if specified path is not directory or path does not exists</returns>
        bool IsDirectory(string path);

        /// <summary>
        /// Whether the directory already exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns>whether specified path is exists and is a directory</returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Whether the directory is empty
        /// </summary>
        /// <param name="path"></param>
        /// <returns>whether the directory is empty</returns>
        bool IsDirectoryEmpty(string path);
        
        /// <summary>
        /// Delete the specified directory
        /// </summary>
        /// <param name="path"></param>
        void DeleteDirectory(string path);

        /// <summary>
        /// Check whether directory can be deleted. Usually use IsDirectoryEmpty
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool DirectoryCanBeDeleted(string path);

        /// <summary>
        /// Delete the specified file
        /// </summary>
        /// <param name="path"></param>
        void DeleteFile(string path);

        /// <summary>
        /// Check whether file is exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool FileExists(string path);

        /// <summary>
        /// Get list of files from directory, Use DokanHelper to match pattern with file name
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        IList<FileInformation> FindFiles(string directory, string searchPattern);

        /// <summary>
        /// Get Dokan's FileInformation structure for a given path.
        /// Path could be directory or file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fi"></param>
        /// <returns></returns>
        bool GetFileInfo(string path, out FileInformation fi);

        /// <summary>
        /// Gets Free Space
        /// </summary>
        /// <param name="freeBytesAvailable"></param>
        /// <param name="totalNumberOfBytes"></param>
        /// <param name="totalNumberOfFreeBytes"></param>
        void GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes);

        /// <summary>
        /// Move Directory
        /// </summary>
        /// <param name="oldPath"></param>
        /// <param name="newPath"></param>
        void MoveDirectory(string oldPath, string newPath);

        /// <summary>
        /// Move File
        /// </summary>
        /// <param name="oldPath"></param>
        /// <param name="newPath"></param>
        void MoveFile(string oldPath, string newPath);

        /// <summary>
        /// Sets the file Attribute
        /// </summary>
        /// <param name="path"></param>
        /// <param name="attr"></param>
        void SetFileAttribute(string path, FileAttributes attr);

        /// <summary>
        /// Sets the File Time
        /// </summary>
        /// <param name="path"></param>
        /// <param name="creationTime"></param>
        /// <param name="lastAccessTime"></param>
        /// <param name="lastWriteTime"></param>
        void SetFileTime(string path, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime);

        /// <summary>
        /// Create an Empty file with given attribute
        /// </summary>
        /// <param name="path"></param>
        /// <param name="attributes"></param>
        /// <returns>IDokanFile which represents the created file</returns>
        NC.DokanFS.IDokanFile Touch(string path, FileAttributes attributes);

        /// <summary>
        /// Update file information
        /// </summary>
        /// <param name="file"></param>
        void UpdateFileInformation(IDokanFile file);

        /// <summary>
        /// Update directory information
        /// </summary>
        /// <param name="file"></param>
        void UpdateDirectoryInformation(IDokanDirectory dir);
    }

}
