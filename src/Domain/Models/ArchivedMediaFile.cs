using Domain.Abstracts;
using Domain.Interfaces;
using Shared.Extensions;
using System.IO.Compression;

namespace Domain.Models;
public sealed class ArchivedMediaFile : AbstractMediaFile
{
    #region Fields
    private readonly ZipArchiveEntry Entry;
    private readonly string ArchiveFileName;
    private readonly DirectoryInfo TempDirectory;
    private readonly bool FixArabicNumbersInName;

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
        FixArabicNumbersInName = settings.AutoFixArabicNumbersInFileName;

        TempDirectory = new DirectoryInfo(Path.Combine(settings.TempZipPath, Guid.NewGuid().ToString()));

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
            var path = Path.Combine(TempDirectory.FullName, FixArabicNumbersInName
                ? Entry.Name.ReplaceArabicNumbers() : Entry.Name);

            archive.GetEntry(AddJsonExtension(Entry.FullName))?.ExtractToFile(AddJsonExtension(path));
            archive.GetEntry(Entry.FullName)!.ExtractToFile(path);

            MediaFile = new MediaFile(new FileInfo(path));
        }
    }
    #endregion

    #region Dispose
    private bool disposed;
    protected override void Dispose(bool disposing)
    {
        if (disposed) return;
        if (disposing)
        {

        }

        if (IsInitialized)
        {
            MediaFile.Dispose();
            TempDirectory.Delete(true);
        }

        disposed = true;
    }
    #endregion
}
