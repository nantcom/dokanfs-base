using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.DokanFS.Specialized
{
    /// <summary>
    /// File context which read a given file in blocks
    /// </summary>
    public abstract class BlockBasedReadOnlyFileContextBase : ReadOnlyFileContextBase
    {
        protected static MemoryCache _BlockCache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 1024 * 1024 * 250 });

        /// <summary>
        /// Get Cache Key for given file
        /// </summary>
        /// <param name="f"></param>
        /// <param name="blockIndex"></param>
        /// <returns></returns>
        protected virtual string GetCacheKey(int blockIndex)
        {
            return $"{this.Disk.Id}-{this.File.Id}-{blockIndex}";
        }

        /// <summary>
        /// Block Request Size
        /// </summary>
        public virtual int BlockSize => 512 * 1024;

        /// <summary>
        /// Maximum size of buffer
        /// </summary>
        public virtual int MaxBufferSize => this.BlockSize * 4;

        /// <summary>
        /// Create new instance of BlockBasedReadOnlyFileContextBase
        /// </summary>
        /// <param name="disk"></param>
        /// <param name="file"></param>
        public BlockBasedReadOnlyFileContextBase(IDokanDisk disk, IDokanFile file) : base (disk, file)
        {

        }


        /// <summary>
        /// Gets block index from offset
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        protected long GetBlockIndex(long offset)
        {
            return offset / this.BlockSize;
        }

        /// <summary>
        /// Gets block index
        /// </summary>
        /// <param name="offsetStart"></param>
        /// <param name="count"></param>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        protected long GetLastBlockIndex(long fileSize, long offsetStart, long count)
        {
            var maxBlock = (int)(fileSize / this.BlockSize);
            return Math.Min((offsetStart + count) / this.BlockSize, maxBlock);
        }

        /// <summary>
        /// Gets MemoryStream that is big enough to hold data to serve the read operation
        /// </summary>
        /// <param name="offset">offset in file</param>
        /// <param name="bufferSize">size of buffer</param>
        /// <returns></returns>
        protected virtual (MemoryStream ms, long startBlock, long endBlock) GetMemoryStream(long offset, long bufferSize = 0)
        {
            var startBlock = this.GetBlockIndex(offset);
            var endBlock = bufferSize == 0 ? startBlock : this.GetLastBlockIndex(_File.FileInformation.Length, offset, bufferSize);

            return
                (
                new MemoryStream(Math.Max(this.BlockSize, (int)(endBlock - startBlock) * this.BlockSize)),
                startBlock,
                endBlock
                );
        }

        /// <summary>
        /// Server Read Operation to Dokan
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, long offset)
        {
            if (offset >= _File.FileInformation.Length)
            {
                return 0;
            }

            if (buffer.Length > this.MaxBufferSize )
            {
                throw new IOException("Buffer is too Large");
            }

            var (ms, startBlock, endBlock) = this.GetMemoryStream(offset, buffer.Length);
            using (ms)
            {
                for (int i = (int)startBlock; i <= endBlock; i++)
                {
                    ms.Write(this.GetBlock(i));
                }

                ms.Position = (long)(offset - (startBlock * this.BlockSize));
                return ms.Read(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Gets a block 
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="reused">Whether this block was reused, which should be cached</param>
        /// <returns></returns>
        protected byte[] GetBlock(int blockIndex, bool reused = false)
        {
            var key = this.GetCacheKey(blockIndex);

            byte[] blockData;
            if (_BlockCache.TryGetValue<byte[]>(key, out blockData))
            {
                return blockData;
            }

            blockData = this.ReadBlockFromStore(blockIndex);

            if (reused)
            {
                _BlockCache.Set<byte[]>(key, blockData,
                    new MemoryCacheEntryOptions() { Size = blockData.Length });
            }

            return blockData;
        }

        /// <summary>
        /// Read Block data from backing store
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <returns></returns>
        protected abstract byte[] ReadBlockFromStore(int blockIndex);
    }
}
