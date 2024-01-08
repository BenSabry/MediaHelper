using Microsoft.Extensions.Configuration;

namespace MediaOrganizer.Core;
public sealed class Settings
{
    #region Fields
    private readonly IConfiguration Configuration;
    #endregion

    #region Properties
    public int TasksCount { get; set; }
    public bool EnableLogAndResume { get; set; }
    public bool AttemptToFixIncorrectOffsets { get; set; }
    public bool ClearBackupFilesOnComplete { get; set; }
    public bool DeleteEmptyDirectoriesOnComplete { get; set; }
    public string Target { get; set; }
    public string[] Sources { get; set; }
    #endregion

    #region Constructors
    public Settings(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    #endregion
}

