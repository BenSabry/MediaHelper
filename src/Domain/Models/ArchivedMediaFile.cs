using Domain.Abstracts;
using Domain.Interfaces;
using System.IO.Compression;

namespace Domain.Models;
public sealed class ArchivedMediaFile : AbstractMediaFile
{
    #region Fields
    private readonly ZipArchiveEntry Entry;
    private readonly string ArchiveFileName;
    private readonly DirectoryInfo TempDirectory;

    private bool IsInitialized;
    private MediaFile MediaFile;

    public override string OriginalSource { get; protected init; }
    #endregion

    #region Constructors
    public ArchivedMediaFile(ZipArchiveEntry item, FileInfo archive, ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(settings);

        IsInitialized = false;
        TempDirectory = Directory.CreateDirectory(Path.Combine(settings.TempZipPath, Guid.NewGuid().ToString()));

        Entry = item;
        ArchiveFileName = archive.FullName;

        OriginalSource = $"{archive.FullName}.{item.FullName.Replace("/", "\\")}";
    }
    #endregion

    #region Destructor
    ~ArchivedMediaFile()
    {
        Dispose(false);
    }
    #endregion

    #region Behavior
    public override FileInfo GetFile()
    {
        Initialize();
        return MediaFile.GetFile();
    }
    public override FileInfo GetJsonFile()
    {
        Initialize();
        return MediaFile.GetJsonFile();
    }
    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && IsInitialized)
        {
            MediaFile.Dispose();
        }
    }

    private void Initialize()
    {
        if (IsInitialized) return;
        IsInitialized = true;

        ExtractArchiveFiles();
    }
    private void ExtractArchiveFiles()
    {
        TempDirectory.Create();

        using (var archive = ZipFile.OpenRead(ArchiveFileName))
        {
            archive.GetEntry(AddJsonExtension(Entry.FullName))?
                .ExtractToFile(Path.Combine(TempDirectory.FullName, AddJsonExtension(Entry.Name)));

            archive.GetEntry(Entry.FullName)!.ExtractToFile(Path.Combine(TempDirectory.FullName, Entry.Name));
            MediaFile = new MediaFile(new FileInfo(Path.Combine(TempDirectory.FullName, Entry.Name)));
        }
    }
    #endregion
}
