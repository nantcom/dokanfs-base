using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using DokanNet;
using DokanNet.Logging;
using static DokanNet.FormatProviders;
using FileAccess = DokanNet.FileAccess;

namespace NC.DokanFS
{
    public class DokanFrontend : IDokanOperations
    {
        private const FileAccess DataAccess = FileAccess.ReadData | FileAccess.WriteData | FileAccess.AppendData |
                                              FileAccess.Execute |
                                              FileAccess.GenericExecute | FileAccess.GenericWrite |
                                              FileAccess.GenericRead;

        private const FileAccess DataWriteAccess = FileAccess.WriteData | FileAccess.AppendData |
                                                   FileAccess.Delete |
                                                   FileAccess.GenericWrite;

        private ConsoleLogger _Logger;

        private IDokanDisk _Backend;

        /// <summary>
        /// Create new Instance
        /// </summary>
        /// <param name="dd"></param>
        /// <param name="loggingName"></param>
        public DokanFrontend(IDokanDisk dd, string loggingName)
        {
            _Backend = dd;
            _Logger = new ConsoleLogger(loggingName);
        }

        /// <summary>
        /// Mount this file system
        /// </summary>
        /// <param name="mountPoint">Mount point, can be drive letter or folder</param>
        /// <param name="readOnly">whether the drive will have WriteProtection flag set</param>
        /// <param name="fixedDisk">whether the drive will have FixedDrive flag set</param>
        /// <param name="useMountManager">whether to use MountManager option</param>
        /// <param name="threads">number of threads, default is 64</param>
        public void Mount( string mountPoint, bool readOnly = false, bool fixedDisk = false, bool useMountManager = false, int threads = 64 )
        {
            DokanOptions opt = fixedDisk ? DokanOptions.FixedDrive : DokanOptions.RemovableDrive;

            if (readOnly)
            {
                opt &= DokanOptions.WriteProtection;
            }

            if (useMountManager)
            {
                opt &= DokanOptions.MountManager;
            }

            this.Mount(mountPoint, opt, threads);
        }

        /// <summary>
        /// Convert OS path into Backend Path
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected string GetPath(string fileName)
        {
            return _Backend.GetPath(fileName);
        }

        protected NtStatus Trace(string method, string fileName, IDokanFileInfo info, NtStatus result,
            params object[] parameters)
        {
#if TRACE
            var extraParameters = parameters != null && parameters.Length > 0
                ? ", " + string.Join(", ", parameters.Select(x => string.Format(DefaultFormatProvider, "{0}", x)))
                : string.Empty;

            if (result != NtStatus.Success)
            {
                _Logger.Debug(DokanFormat($"{method}('{fileName}', {info}{extraParameters}) -> {result}"));
            }

#endif

            return result;
        }

        private NtStatus Trace(string method, string fileName, IDokanFileInfo info,
            FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes,
            NtStatus result)
        {
#if TRACE

            if (result != NtStatus.Success)
            {

                _Logger.Debug(
                    DokanFormat(
                        $"{method}('{fileName}', {info}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}"));
            }
#endif

            return result;
        }

        #region Implementation of IDokanOperations

        public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode,
            FileOptions options, FileAttributes attributes, IDokanFileInfo info)
        {
            var result = DokanResult.Success;
            var filePath = GetPath(fileName);

            if (info.IsDirectory)
            {
                try
                {
                    switch (mode)
                    {
                        case FileMode.Open:
                            if (!_Backend.DirectoryExists(filePath))
                            {
                                if (!_Backend.IsDirectory(filePath))
                                {
                                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                        attributes, DokanResult.NotADirectory);
                                }

                                return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                    attributes, DokanResult.PathNotFound);
                            }

                            break;

                        case FileMode.CreateNew:
                            if (_Backend.DirectoryExists(filePath))
                                return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                    attributes, DokanResult.FileExists);

                            _Backend.CreateDirectory(GetPath(fileName));
                            break;
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                        DokanResult.AccessDenied);
                }
            }
            else
            {
                var pathExists = true;
                var pathIsDirectory = false;

                var readWriteAttributes = (access & DataAccess) == 0;
                var readAccess = (access & DataWriteAccess) == 0;

                pathExists = (_Backend.DirectoryExists(filePath) || _Backend.FileExists(filePath));
                pathIsDirectory = pathExists ? _Backend.IsDirectory(filePath) : false;

                switch (mode)
                {
                    case FileMode.Open:

                        if (pathExists)
                        {
                            // check if driver only wants to read attributes, security info, or open directory
                            if (readWriteAttributes || pathIsDirectory)
                            {
                                if (pathIsDirectory && (access & FileAccess.Delete) == FileAccess.Delete
                                    && (access & FileAccess.Synchronize) != FileAccess.Synchronize)
                                {
                                    //It is a DeleteFile request on a directory
                                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                        attributes, DokanResult.AccessDenied);
                                }

                                info.IsDirectory = pathIsDirectory;
                                info.Context = new object();
                                // must set it to something if you return DokanError.Success

                                return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                    attributes, DokanResult.Success);
                            }
                        }
                        else
                        {
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                DokanResult.FileNotFound);
                        }
                        break;

                    case FileMode.CreateNew:
                        if (pathExists)
                        {
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                DokanResult.FileExists);
                        }
                        break;

                    case FileMode.Truncate:
                        if (!pathExists)
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                DokanResult.FileNotFound);
                        break;
                }

                try
                {
                    bool fileCreated = mode == FileMode.CreateNew || mode == FileMode.Create || (!pathExists && mode == FileMode.OpenOrCreate);
                    if (fileCreated)
                    {
                        FileAttributes new_attributes = attributes;
                        new_attributes |= FileAttributes.Archive; // Files are always created as Archive
                        // FILE_ATTRIBUTE_NORMAL is override if any other attribute is set.
                        new_attributes &= ~FileAttributes.Normal;

                        _Backend.Touch(filePath, new_attributes);
                    }

                    info.Context = _Backend.CreateFileContext(filePath, mode,
                        readAccess ? System.IO.FileAccess.Read : System.IO.FileAccess.ReadWrite, share, options);

                    if (pathExists && (mode == FileMode.OpenOrCreate
                                       || mode == FileMode.Create))
                    {
                        result = DokanResult.AlreadyExists;
                    }
                }
                catch (UnauthorizedAccessException) // don't have access rights
                {
                    if (info.Context is IDisposable mx)
                    {
                        // returning AccessDenied cleanup and close won't be called,
                        // so we have to take care of the stream now
                        mx.Dispose();
                        info.Context = null;
                    }
                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                        DokanResult.AccessDenied);
                }
                catch (DirectoryNotFoundException)
                {
                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                        DokanResult.PathNotFound);
                }
                catch (Exception)
                {
                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                DokanResult.InternalError);
                }
            }
            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                result);
        }

        public void Cleanup(string fileName, IDokanFileInfo info)
        {
#if TRACE
            if (info.Context != null)
                Console.WriteLine(DokanFormat($"{nameof(Cleanup)}('{fileName}', {info} - entering"));
#endif

            (info.Context as IDokanFileContext)?.Dispose();
            info.Context = null;

            if (info.DeleteOnClose)
            {
                try
                {
                    if (info.IsDirectory)
                    {
                        _Backend.DeleteDirectory(GetPath(fileName));
                    }
                    else
                    {
                        _Backend.DeleteFile(GetPath(fileName));
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Trace(nameof(Cleanup), fileName, info, DokanResult.AccessDenied);
                }
            }
            Trace(nameof(Cleanup), fileName, info, DokanResult.Success);
        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {
#if TRACE
            if (info.Context != null)
                Console.WriteLine(DokanFormat($"{nameof(CloseFile)}('{fileName}', {info} - entering"));
#endif

            (info.Context as IDokanFileContext)?.Dispose();
            info.Context = null;
            Trace(nameof(CloseFile), fileName, info, DokanResult.Success);
            // could recreate cleanup code here but this is not called sometimes
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            try
            {
                if (info.Context == null) // memory mapped read
                {
                    using (var mx = _Backend.CreateFileContext(GetPath(fileName), FileMode.Open, System.IO.FileAccess.Read))
                    {
                        bytesRead = mx.Read(buffer, offset);
                    }
                }
                else // normal read
                {
                    var mx = info.Context as IDokanFileContext;
                    lock (mx) //Protect from overlapped read
                    {
                        bytesRead = mx.Read(buffer, offset);
                    }
                }
            }
            catch (Exception)
            {
                bytesRead = 0;
                return Trace(nameof(ReadFile), fileName, info, DokanResult.InvalidParameter, "0",
                    offset.ToString(CultureInfo.InvariantCulture));
            }

            return Trace(nameof(ReadFile), fileName, info, DokanResult.Success, "out " + bytesRead.ToString(),
                offset.ToString(CultureInfo.InvariantCulture));
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
        {
            var append = offset == -1;
            if (info.Context == null)
            {
                using (var mx = _Backend.CreateFileContext(GetPath(fileName), append ? FileMode.Append : FileMode.Open, System.IO.FileAccess.Write))
                {
                    if (!append) // Offset of -1 is an APPEND: https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-writefile
                    {
                        mx.Write(buffer, offset);
                    }
                    else
                    {
                        mx.Append(buffer);
                    }
                    bytesWritten = buffer.Length;
                }
            }
            else
            {
                var mx = info.Context as IDokanFileContext;
                lock (mx) //Protect from overlapped write
                {
                    if (append)
                    {
                        mx.Append(buffer);
                    }
                    else
                    {
                        mx.Write(buffer, offset);
                    }
                }
                bytesWritten = buffer.Length;
            }
            return Trace(nameof(WriteFile), fileName, info, DokanResult.Success, "out " + bytesWritten.ToString(),
                offset.ToString(CultureInfo.InvariantCulture));
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
        {
            try
            {
                ((IDokanFileContext)(info.Context)).Flush();
                return Trace(nameof(FlushFileBuffers), fileName, info, DokanResult.Success);
            }
            catch (IOException)
            {
                return Trace(nameof(FlushFileBuffers), fileName, info, DokanResult.DiskFull);
            }
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
        {
            // may be called with info.Context == null, but usually it isn't
            var filePath = GetPath(fileName);

            var success =_Backend.GetFileInfo(filePath, out fileInfo);
            if (success == false)
            {
                return Trace(nameof(GetFileInformation), fileName, info, info.IsDirectory ? DokanResult.PathNotFound : DokanResult.FileNotFound);
            }

            return Trace(nameof(GetFileInformation), fileName, info, DokanResult.Success);
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            // This function is not called because FindFilesWithPattern is implemented
            // Return DokanResult.NotImplemented in FindFilesWithPattern to make FindFiles called
            files = _Backend.FindFiles(fileName, "*");

            return Trace(nameof(FindFiles), fileName, info, DokanResult.Success);
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
        {
            if (attributes == 0)
            {
                return DokanResult.Success;
            }

            try
            {
                _Backend.SetFileAttribute(fileName, attributes);
            }
            catch (UnauthorizedAccessException)
            {
                return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.AccessDenied, attributes.ToString());
            }
            catch (FileNotFoundException)
            {
                return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.FileNotFound, attributes.ToString());
            }

            return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.Success, attributes.ToString());
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime,
            DateTime? lastWriteTime, IDokanFileInfo info)
        {
            try
            {
                var filePath = GetPath(fileName);
                _Backend.SetFileTime(filePath, creationTime, lastAccessTime, lastWriteTime);

                return Trace(nameof(SetFileTime), fileName, info, DokanResult.Success, creationTime, lastAccessTime,
                    lastWriteTime);
            }
            catch (UnauthorizedAccessException)
            {
                return Trace(nameof(SetFileTime), fileName, info, DokanResult.AccessDenied, creationTime, lastAccessTime,
                    lastWriteTime);
            }
            catch (FileNotFoundException)
            {
                return Trace(nameof(SetFileTime), fileName, info, DokanResult.FileNotFound, creationTime, lastAccessTime,
                    lastWriteTime);
            }
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            var filePath = GetPath(fileName);

            if (_Backend.DirectoryExists(filePath))
                return Trace(nameof(DeleteFile), fileName, info, DokanResult.AccessDenied);

            if (!_Backend.FileExists(filePath))
                return Trace(nameof(DeleteFile), fileName, info, DokanResult.FileNotFound);

            if (_Backend.IsDirectory(filePath))
                return Trace(nameof(DeleteFile), fileName, info, DokanResult.AccessDenied);

            return Trace(nameof(DeleteFile), fileName, info, DokanResult.Success);
            // we just check here if we could delete the file - the true deletion is in Cleanup
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            return Trace(nameof(DeleteDirectory), fileName, info,
                _Backend.IsDirectoryEmpty(fileName)
                    ? DokanResult.DirectoryNotEmpty
                    : DokanResult.Success);
            // if dir is not empty it can't be deleted
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
        {
            var oldpath = GetPath(oldName);
            var newpath = GetPath(newName);

            (info.Context as IDokanFileContext)?.Dispose();
            info.Context = null;

            var exist = info.IsDirectory ? _Backend.DirectoryExists(newpath) : _Backend.FileExists(newpath);

            try
            {

                if (!exist)
                {
                    info.Context = null;
                    if (info.IsDirectory)
                    {
                        _Backend.MoveDirectory(oldpath, newpath);
                    }
                    else
                    {
                        _Backend.MoveFile(oldpath, newpath);
                    }
                    return Trace(nameof(MoveFile), oldName, info, DokanResult.Success, newName,
                        replace.ToString(CultureInfo.InvariantCulture));
                }
                else if (replace)
                {
                    info.Context = null;

                    if (info.IsDirectory) //Cannot replace directory destination - See MOVEFILE_REPLACE_EXISTING
                        return Trace(nameof(MoveFile), oldName, info, DokanResult.AccessDenied, newName,
                            replace.ToString(CultureInfo.InvariantCulture));

                    _Backend.DeleteFile(newpath);
                    _Backend.MoveFile(oldpath, newpath);

                    return Trace(nameof(MoveFile), oldName, info, DokanResult.Success, newName,
                        replace.ToString(CultureInfo.InvariantCulture));
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Trace(nameof(MoveFile), oldName, info, DokanResult.AccessDenied, newName,
                    replace.ToString(CultureInfo.InvariantCulture));
            }
            return Trace(nameof(MoveFile), oldName, info, DokanResult.FileExists, newName,
                replace.ToString(CultureInfo.InvariantCulture));
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            try
            {
                ((IDokanFileContext)(info.Context)).SetLength(length);
                return Trace(nameof(SetEndOfFile), fileName, info, DokanResult.Success,
                    length.ToString(CultureInfo.InvariantCulture));
            }
            catch (IOException)
            {
                return Trace(nameof(SetEndOfFile), fileName, info, DokanResult.DiskFull,
                    length.ToString(CultureInfo.InvariantCulture));
            }
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            try
            {
                ((IDokanFileContext)(info.Context)).SetLength(length);
                return Trace(nameof(SetAllocationSize), fileName, info, DokanResult.Success,
                    length.ToString(CultureInfo.InvariantCulture));
            }
            catch (IOException)
            {
                return Trace(nameof(SetAllocationSize), fileName, info, DokanResult.DiskFull,
                    length.ToString(CultureInfo.InvariantCulture));
            }
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            try
            {
                ((IDokanFileContext)(info.Context)).Lock(offset, length);
                return Trace(nameof(LockFile), fileName, info, DokanResult.Success,
                    offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
            }
            catch (IOException)
            {
                return Trace(nameof(LockFile), fileName, info, DokanResult.AccessDenied,
                    offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
            }
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            try
            {
                ((IDokanFileContext)(info.Context)).Unlock(offset, length);
                return Trace(nameof(UnlockFile), fileName, info, DokanResult.Success,
                    offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
            }
            catch (IOException)
            {
                return Trace(nameof(UnlockFile), fileName, info, DokanResult.AccessDenied,
                    offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
            }
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, IDokanFileInfo info)
        {
            _Backend.GetDiskFreeSpace(out freeBytesAvailable, out totalNumberOfBytes, out totalNumberOfFreeBytes);

            return Trace(nameof(GetDiskFreeSpace), null, info, DokanResult.Success, "out " + freeBytesAvailable.ToString(),
                "out " + totalNumberOfBytes.ToString(), "out " + totalNumberOfFreeBytes.ToString());
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features,
            out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
        {
            volumeLabel = "NC Cloud Space Beta";
            fileSystemName = "NC Cloud Space Beta";
            maximumComponentLength = 256;

            features = FileSystemFeatures.VolumeIsCompressed;

            return Trace(nameof(GetVolumeInformation), null, info, DokanResult.Success, "out " + volumeLabel,
                "out " + features.ToString(), "out " + fileSystemName);
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections,
            IDokanFileInfo info)
        {
            security = null;
            return DokanResult.NotImplemented;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections,
            IDokanFileInfo info)
        {
            return DokanResult.NotImplemented;
        }

        public NtStatus Mounted(IDokanFileInfo info)
        {
            return Trace(nameof(Mounted), null, info, DokanResult.Success);
        }

        public NtStatus Unmounted(IDokanFileInfo info)
        {
            return Trace(nameof(Unmounted), null, info, DokanResult.Success);
        }

        public NtStatus FindStreams(string fileName, IntPtr enumContext, out string streamName, out long streamSize,
            IDokanFileInfo info)
        {
            streamName = string.Empty;
            streamSize = 0;
            return Trace(nameof(FindStreams), fileName, info, DokanResult.NotImplemented, enumContext.ToString(),
                "out " + streamName, "out " + streamSize.ToString());
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            streams = new FileInformation[0];
            return Trace(nameof(FindStreams), fileName, info, DokanResult.NotImplemented);
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files,
            IDokanFileInfo info)
        {
            files = _Backend.FindFiles(fileName, searchPattern);

            return Trace(nameof(FindFilesWithPattern), fileName, info, DokanResult.Success);
        }

        #endregion Implementation of IDokanOperations
    }
}