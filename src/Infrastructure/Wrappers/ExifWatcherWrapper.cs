using Domain.Interfaces;
using Shared.Helpers;

namespace Infrastructure.Wrappers;
public sealed class ExifWatcherWrapper
{
    #region Fields-Static
    public const string ToolName = "ExifWatcher.exe";
    private static bool Initialized;

    public static string Version { get; private set; } = "0.0.0";
    #endregion

    #region Fields-Instance
    private bool IsRunning;
    private readonly string path;
    #endregion

    #region Constructors
    public ExifWatcherWrapper(ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        path = Path.Combine(settings.ToolsDirectory, ToolName);

        Initialize(path);
    }
    #endregion

    #region Behavior-Static
    private static void Initialize(string path)
    {
        if (Initialized) return;
        Initialized = true;

        if (!File.Exists(path))
            throw new FileNotFoundException($"{ToolName} is missing!");

        Version = ProcessHelper.RunAndGetOutput(path, "ver").Trim();
    }
    #endregion

    #region Behavior-Instance
    public void StartWatching()
    {
        if (IsRunning) return;
        IsRunning = true;

        ProcessHelper.RunAsync(path, string.Empty).ConfigureAwait(false);
    }
    #endregion
}
