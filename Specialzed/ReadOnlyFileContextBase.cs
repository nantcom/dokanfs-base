using DokanNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.DokanFS.Specialized
{
    public abstract class ReadOnlyFileContextBase : IDokanFileContext
    {
        protected IDokanFile _File;
        protected IDokanDisk _Disk;

        /// <summary>
        /// File that this context will handle
        /// </summary>
        public IDokanFile File => _File;

        /// <summary>
        /// Disk that owns this context
        /// </summary>
        public IDokanDisk Disk => _Disk;

        /// <summary>
        /// Create new instance of ReadOnlyFileContextBase
        /// </summary>
        /// <param name="disk"></param>
        /// <param name="file"></param>
        public ReadOnlyFileContextBase(IDokanDisk disk, IDokanFile file )
        {
            _Disk = disk;
            _File = file;
        }

        public abstract void Dispose();

        public virtual void Flush()
        {

        }

        public abstract int Read(byte[] buffer, long offset);

        #region Not Supported Operations

        /// <summary>
        /// Not Supported, Throws IOException
        /// </summary>
        /// <param name="buffer"></param>
        public virtual void Append(byte[] buffer)
        {
            throw new IOException();
        }

        /// <summary>
        /// Not Supported, Throws IOException
        /// </summary>
        public virtual void Lock(long offset, long length)
        {
            throw new IOException();
        }

        /// <summary>
        /// Not Supported, Throws IOException
        /// </summary>
        public virtual void Unlock(long offset, long length)
        {
            throw new IOException();
        }

        /// <summary>
        /// Not Supported, Throws IOException
        /// </summary>
        public virtual void SetLength(long length)
        {
            throw new IOException();
        }

        /// <summary>
        /// Not Supported, Throws IOException
        /// </summary>
        public virtual void Write(byte[] buffer, long offset)
        {
            throw new IOException();
        }

        #endregion
    }
}
