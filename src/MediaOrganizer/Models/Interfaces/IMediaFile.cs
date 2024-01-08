namespace MediaOrganizer.Models.Interfaces;
public interface IMediaFile : IDisposable
{
    public bool HasRelativeJson { get; }
    public string OriginalSource { get; }

    public FileInfo GetFile();
    public FileInfo GetJsonFile();
}
