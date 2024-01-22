using Domain.Interfaces;

namespace Domain.Abstracts;
public abstract class AbstractMediaFile : IMediaFile
{
    #region Fields
    public abstract string OriginalSource { get; protected init; }
    #endregion

    #region Behavior
    public abstract FileInfo GetFile();
    public abstract FileInfo GetJsonFile();
    public abstract void Dispose();
    protected abstract void Dispose(bool disposing);

    protected static string AddJsonExtension(string path) => $"{path}.json";
    #endregion
}
