namespace Application.Infrastructure.Services;

public interface IExifService : IExifCoreService, IDisposable
{
    Dictionary<string, string> ReadMetadata(string path);
    Dictionary<string, string> ReadJsonMetadata(string path);

    bool TryWriteMetadata(string path, Dictionary<string, string> tags);
}
