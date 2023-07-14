using System;
using System.Threading;

namespace SimpleThreadMonitor
{
    /// <summary>
    /// from 
    /// <para>https://github.com/uhm0311/SimpleThreadMonitor</para>
    /// <para>https://www.nuget.org/packages/SimpleThreadMonitor/1.0.2.1</para>
    /// </summary>
    public static class SimpleMutexThreadMonitor
    {
        public static void Lock(object Object, Action Process, Action<Exception> ExceptionCallback = null, bool ReleaseLockBeforeExceptionCallback = false)
        {
            if (Object == null || Process == null)
            {
                return;
            }

            try
            {
                Monitor.Enter(Object);
                Process();
                Monitor.Exit(Object);
            }
            catch (Exception obj)
            {
                if (ReleaseLockBeforeExceptionCallback)
                {
                    Monitor.Exit(Object);
                }

                try
                {
                    ExceptionCallback?.Invoke(obj);
                }
                catch
                {
                }

                if (!ReleaseLockBeforeExceptionCallback)
                {
                    Monitor.Exit(Object);
                }
            }
        }
    }
}