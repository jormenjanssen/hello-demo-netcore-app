using System;
using System.Threading;

namespace HelloDemo.Server
{
    public static class PollHelper
    {
        public static void PollUntil(Func<bool> pollFunc, TimeSpan checkTimeSpan, TimeSpan maxTimeSpan)
        {
            var beginTime = DateTime.UtcNow;

            while (beginTime.Add(maxTimeSpan) > DateTime.UtcNow)
            {
                if (!pollFunc())
                {
                    Thread.Sleep(checkTimeSpan);
                }
                else
                {
                    return;
                }
            }
        }
    }
}
