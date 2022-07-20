using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.DokanFS
{
    public enum DriveLetters
    {
        a,
        b,
        c,
        d,
        e,
        f,
        g,
        h,
        i,
        j,
        k,
        l,
        m,
        n,
        o,
        p,
        q,
        r,
        s,
        t,
        u,
        v,
        w,
        x,
        y,
        z
    }
    public class MountParameters
    {
        public DriveLetters DriveLetter { get; set; }
        public string VolumeLabel { get; set; }
    }
}
