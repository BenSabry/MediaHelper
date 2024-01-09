using MediaOrganizer.Core;
using MediaOrganizer.Helpers;

try
{
    Engine.Run();
}
catch (Exception ex)
{
    LogHelper.Error($"\n{ex.Message}");
    LogHelper.Notice(ex.StackTrace);
}

LogHelper.Warning("\nPress any key to exit.");
LogHelper.ReadKey();
