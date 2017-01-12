using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CtxPackageAll.CodeGenerators
{
    public class Logger
    {
        static readonly bool enabled = false;
        static readonly string FilePath = string.Empty;

        static Logger()
        {
            FilePath = Path.Combine(Path.GetDirectoryName(typeof(Logger).Assembly.Location), "log.txt");
        }

        public static void Log(string message)
        {
            if (enabled)
            {
                using (var streamWriter = new StreamWriter(FilePath, true))
                {
                    streamWriter.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToString("yyyyMMdd-HHmmss"), message));
                }
            }
        }
    }
}
