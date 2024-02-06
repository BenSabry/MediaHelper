using Domain.Interfaces;
using Shared.Helpers;

namespace Infrastructure.Wrappers;
public sealed class ExifWatcherWrapper
{
    #region Fields
    public const string ToolName = "ExifWatcher.exe";

    private readonly string path;
    private bool IsRunning;

    public string Version { get; private init; } = "0.0.0";
    #endregion

    #region Constructors
    public ExifWatcherWrapper(ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        path = Path.Combine(settings.ToolsDirectory, ToolName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"{ToolName} is missing!");

        Version = ProcessHelper.RunAndGetOutput(path, "ver").Trim();
    }
    #endregion

    #region Behavior
    public void StartWatching()
    {
        if (IsRunning) return;
        IsRunning = true;

        ProcessHelper.RunAsync(path)
            .ConfigureAwait(false);
    }
    #endregion
}
