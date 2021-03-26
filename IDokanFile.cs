using DokanNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.DokanFS
{
    public interface IDokanFile
    {
        /// <summary>
        /// File information for this file
        /// </summary>
        FileInformation FileInformation { get; set; }

        /// <summary>
        /// Unique Identifier of this file
        /// </summary>
        string Id { get; set; }
    }
}
