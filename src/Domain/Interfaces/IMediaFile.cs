namespace Domain.Interfaces;
public interface IMediaFile : IDisposable
{
    public string OriginalSource { get; }

    public FileInfo GetFile();
    public FileInfo GetJsonFile();
}
