using DokanNet;
using DokanNet.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NC.DokanFS
{
    public abstract class MountableBase
    {
        private System.Threading.ManualResetEvent _mre;

        public void Mount(MountParameters mountParameters, DokanFrontend frontend, ILogger logger = null)
        {
            ILogger loggerToUse = logger;
            if(loggerToUse == null)
                loggerToUse = new NullLogger();

            using (_mre = new System.Threading.ManualResetEvent(false))
            using (var dokan = new Dokan(loggerToUse))
            {
                var dokanBuilder = new DokanInstanceBuilder(dokan)
                       .ConfigureOptions(options =>
                       {
                           options.Options = DokanOptions.DebugMode | DokanOptions.StderrOutput;
                           options.MountPoint = mountParameters.DriveLetter.ToString() + ":\\";
                       });
                using (var dokanInstance = dokanBuilder.Build(frontend))
                {
                    _mre.WaitOne();
                }
            }
        }

        public void Unmount()
        {
            _mre.Set();
        }
    }
}
