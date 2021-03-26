using Microsoft.Extensions.Caching.Memory;
using NC.DokanFS.Specialized;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.DokanFS.Specialzed
{
    public abstract class BlockBasedFileContextBase : BlockBasedReadOnlyFileContextBase
    {
        /// <summary>
        /// Create new instance of BlockBasedReadOnlyFileContextBase
        /// </summary>
        /// <param name="disk"></param>
        /// <param name="file"></param>
        public BlockBasedFileContextBase(IDokanDisk disk, IDokanFile file) : base(disk, file)
        {

        }

        /// <summary>
        /// Set data of given block index
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="data"></param>
        public void SetBlock(int blockIndex, byte[] blockData)
        {
            var key = this.GetCacheKey(blockIndex);

            // update cache if this block was cached
            byte[] temp;
            if (_BlockCache.TryGetValue<byte[]>(key, out temp))
            {
                _BlockCache.Set<byte[]>(key, blockData,
                    new MemoryCacheEntryOptions() { Size = blockData.Length });

                // write block will cache if the block was reused
                return;
            }

            this.WriteBlockToStore(blockIndex, blockData);
        }

        /// <summary>
        /// Flush the cached block to backing store, if cached
        /// </summary>
        /// <param name="blockIndex"></param>
        public void FlushBlock(int blockIndex)
        {
            var key = this.GetCacheKey(blockIndex);

            byte[] blockData;
            if (_BlockCache.TryGetValue(key, out blockData))
            {
                this.WriteBlockToStore(blockIndex, blockData);
                _BlockCache.Remove(key);
            }
        }

        /// <summary>
        /// When overridden, this method should write the block data to backing store
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="data"></param>
        public abstract void WriteBlockToStore(int blockIndex, byte[] blockData);

        protected bool _LengthUpdated = false;
        protected HashSet<int> _DirtyBlocks = new();

        /// <summary>
        /// Write to file at given offset
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public override void Write(byte[] buffer, long offset)
        {
            // if writing data beyond end of file, update the length information
            var newLength = offset + buffer.Length;
            if (newLength > _File.FileInformation.Length)
            {
                var fi = _File.FileInformation;
                fi.Length = newLength;
                _File.FileInformation = fi;
                _LengthUpdated = true;
            }

            var (ms, startBlock, endBlock) = this.GetMemoryStream(offset, buffer.Length);
            var startPosition = this.BlockSize * startBlock;

            using (ms)
            {
                for (int i = (int)startBlock; i <= endBlock; i++)
                {
                    byte[] data = this.GetBlock(i, _DirtyBlocks.Contains(i));

                    if (data != null)
                    {
                        ms.Write(data);
                    }
                    else
                    {
                        // the block does not yet exists on storage
                        // virtually write the data into memorystream
                        ms.SetLength(ms.Position + this.BlockSize);
                        ms.Position += this.BlockSize;
                    }
                }

                // overwrite the data of the block
                ms.Position = (long)(offset - (startBlock * this.BlockSize));
                ms.Write(buffer);

                // truncate the memorystream so that it reflect the actual length
                if (startPosition + ms.Length > _File.FileInformation.Length)
                {
                    ms.SetLength(_File.FileInformation.Length - startPosition);
                }

                // read back the data in blocks
                ms.Position = 0;
                for (int i = (int)startBlock; i <= endBlock; i++)
                {
                    var blockData = new byte[this.BlockSize];
                    var lastRead = ms.Read(blockData);

                    if (lastRead < blockData.Length)
                    {
                        // end block
                        var finalBuffer = new byte[lastRead];
                        Array.Copy(blockData, finalBuffer, lastRead);

                        blockData = finalBuffer;
                    }

                    _DirtyBlocks.Add(i);
                    this.SetBlock(i, blockData);
                }

            }
        }

        /// <summary>
        /// Dispose this instance
        /// </summary>
        public override void Dispose()
        {
            this.Flush();

            if (_LengthUpdated)
            {
                _Disk.UpdateFileInformation(_File);
            }
        }

        /// <summary>
        /// Flush cached blocks to store
        /// </summary>
        public override void Flush()
        {
            lock (this)
            {
                foreach (var blockIndex in _DirtyBlocks)
                {
                    this.FlushBlock(blockIndex);
                }
            }
        }
    }
}
