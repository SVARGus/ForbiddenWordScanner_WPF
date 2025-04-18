using System.Threading;

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
