using System.Diagnostics;

namespace MediaHelper.Tests;
public class Helper
{
    public static bool DidProcessStartInTimeRange(string processName, int waitTimeInMilliseconds, int delayInMillisecods = 10)
    {
        for (int i = 0; i < waitTimeInMilliseconds; i += delayInMillisecods)
        {
            if (Process.GetProcessesByName(processName).Length > 0)
                return true;

            Task.Delay(delayInMillisecods).Wait();
        }

        return false;
    }
}
