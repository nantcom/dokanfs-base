using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.DokanFS
{
    public interface IDokanFileContext : IDisposable
    {
        /// <summary>
        /// Read data into given buffer at given offset
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        int Read(byte[] buffer, long offset);

        /// <summary>
        /// Write data from given buffer at given offset
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        void Write(byte[] buffer, long offset);

        /// <summary>
        /// Append data in given buffer to end of file
        /// </summary>
        /// <param name="buffer"></param>
        void Append(byte[] buffer);

        /// <summary>
        /// Flush the internal buffer, called before file closes
        /// </summary>
        void Flush();

        /// <summary>
        /// Set Length of file
        /// </summary>
        /// <param name="length"></param>
        void SetLength(long length);

        /// <summary>
        /// Lock given part of the file
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void Lock(long offset, long length);

        /// <summary>
        /// Unlock given part of the file
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void Unlock(long offset, long length);
    }
}
