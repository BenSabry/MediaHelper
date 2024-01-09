using MediaOrganizer.Helpers;
using MediaOrganizer.Models.Abstracts;
using System.IO.Compression;

namespace MediaOrganizer.Models;
public sealed class ArchivedMediaFile : AbstractMediaFile
{
    #region Fields-Static
    private static readonly DirectoryInfo SharedTempDirectory = new DirectoryInfo(Path.Combine(CommonHelper.TempDirectory, @"Extracted"));
    #endregion

    #region Fields-Instance
    private readonly ZipArchiveEntry Entry;
    private readonly string ArchiveFileName;

    private bool IsInitialized;
    private DirectoryInfo CurrentTempDirectory;
    private MediaFile MediaFile;

    public override string OriginalSource { get; protected set; }
    #endregion

    #region Constructors
    public ArchivedMediaFile(ZipArchiveEntry item, FileInfo archive)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(archive);

        IsInitialized = false;

        Entry = item;
        ArchiveFileName = archive.FullName;

        OriginalSource = $"{archive.FullName}.{item.FullName}";
    }
    #endregion

    #region Destructor
    ~ArchivedMediaFile()
    {
        Dispose();
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
        MediaFile.Dispose();
        CurrentTempDirectory.Delete(true);
    }

    private void Initialize()
    {
        if (IsInitialized) return;
        IsInitialized = true;

        ExtractArchiveFiles();
    }
    private void ExtractArchiveFiles()
    {
        CurrentTempDirectory = Directory.CreateDirectory(
            Path.Combine(SharedTempDirectory.FullName,
            Guid.NewGuid().ToString()));

        CurrentTempDirectory.Create();

        using (var archive = ZipFile.OpenRead(ArchiveFileName))
        {
            archive.GetEntry(AddJsonExtension(Entry.FullName))?
                .ExtractToFile(Path.Combine(CurrentTempDirectory.FullName, AddJsonExtension(Entry.Name)));

            archive.GetEntry(Entry.FullName)!.ExtractToFile(Path.Combine(CurrentTempDirectory.FullName, Entry.Name));
            MediaFile = new MediaFile(new FileInfo(Path.Combine(CurrentTempDirectory.FullName, Entry.Name)));
        }
    }
    internal static void CleanUp()
    {
        if (SharedTempDirectory.Exists)
            SharedTempDirectory.Delete(true);
    }
    #endregion
}
