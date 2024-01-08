using MediaOrganizer.Models.Interfaces;

namespace MediaOrganizer.Models.Abstracts;
public abstract class AbstractMediaFile : IMediaFile
{
    #region Fields
    public bool HasRelativeJson => GetJsonFile() is not null && GetJsonFile().Exists;
    public abstract string OriginalSource { get; protected set; }
    #endregion

    #region Behavior
    public abstract FileInfo GetFile();
    public abstract FileInfo GetJsonFile();
    public abstract void Dispose();

    protected static string AddJsonExtension(string path) => $"{path}.json";
    #endregion
}
