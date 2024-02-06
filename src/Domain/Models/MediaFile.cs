using Domain.Abstracts;

namespace Domain.Models;
public sealed class MediaFile : AbstractMediaFile
{
    #region Fields
    private readonly FileInfo FileEntry;
    private readonly FileInfo JsonEntry;

    public override string OriginalSource { get; protected init; }
    #endregion

    #region Constructors
    public MediaFile(FileInfo item)
    {
        ArgumentNullException.ThrowIfNull(item);

        FileEntry = item;
        JsonEntry = new FileInfo(AddJsonExtension(item.FullName));

        OriginalSource = item.FullName;
    }
    #endregion

    #region Behavior
    public override FileInfo GetFile() => FileEntry;
    public override FileInfo GetJsonFile() => JsonEntry;
    #endregion

    #region Dispose
    private bool disposed;
    protected override void Dispose(bool disposing)
    {
        if (!disposed) return;
        if (disposing)
        {

        }

        disposed = true;
    }
    #endregion
}