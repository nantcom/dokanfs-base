using DokanNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.DokanFS
{
    public interface IDokanDirectory
    {
        /// <summary>
        /// Directory information for this directory, should have Directory Bit set
        /// </summary>
        FileInformation FileInformation { get; }
    }
}
