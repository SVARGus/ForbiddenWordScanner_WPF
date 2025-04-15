using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ForbiddenWordScanner_WPF
{
    public static class SingleInstance
    {
        private static Mutex _mutex;
        public static bool EnsureSingleInstance()
        {
            bool createdNew;
            _mutex = new Mutex(true, "ForbiddenWordScanner_SingleInstance", out createdNew);
            return createdNew;
        }
    }
}
