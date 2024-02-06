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
    
    protected static string AddJsonExtension(string path) => $"{path}.json";
    #endregion

    #region Dispose
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected abstract void Dispose(bool disposing);
    #endregion
}
