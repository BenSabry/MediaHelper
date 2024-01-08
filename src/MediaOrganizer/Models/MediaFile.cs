using MediaOrganizer.Models.Abstracts;

namespace MediaOrganizer.Models;
public sealed class MediaFile : AbstractMediaFile
{
    #region Fields
    private readonly FileInfo FileEntry;
    private readonly FileInfo JsonEntry;

    public override string OriginalSource { get; protected set; }
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

    public override void Dispose() { }
    #endregion
}