using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.DokanFS
{
    public class ReadOnlyFileContextBase : IDokanFileContext
    {
        public void Append(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public abstract void Dispose();

        public virtual void Flush()
        {
            
        }

        public void Lock(long offset, long length)
        {
            throw new IOException();
        }

        public void Unlock(long offset, long length)
        {
            throw new IOException();
        }

        public int Read(byte[] buffer, long offset)
        {
            throw new NotImplementedException();
        }

        public void SetLength(long length)
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] buffer, long offset)
        {
            throw new IOException();
        }
    }
}
