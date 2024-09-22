namespace SimpleDatabase.Utility;

/// <summary>
///  Manage reading and writing pages(data) to the disk
/// </summary>
public class Pager : IDisposable
{
    /// <summary>
    ///  Individual page size in the unit of bytes
    /// </summary>
    public const int PageSize = 4096;

    public const uint TableMaxPages = 400;

    private FileStream _fileStream;
    private Dictionary<uint, byte[]> _pages = new Dictionary<uint, byte[]>();

    /// <summary>
    ///  Split the file by page size into multiple smaller chunks to manage
    /// </summary>
    public uint NumPages { get; private set; }

    /// <summary>
    ///  Load the file and get a variable saving the page indexes
    /// </summary>
    /// <param name="filename"></param>
    public Pager(string filename)
    {
        _fileStream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        NumPages = (uint)(_fileStream.Length / PageSize);
    }

    public byte[] GetPage(uint pageNum)
    {
        if (pageNum > TableMaxPages)
        {
            throw new Exception($"Tried to fetch page number out of bounds. {pageNum}");
        }

        if (!_pages.ContainsKey(pageNum))
        {
            byte[] page = new byte[PageSize];
            long numPages = _fileStream.Length / PageSize;

            if (pageNum < numPages)
            {
                _fileStream.Seek(pageNum * PageSize, SeekOrigin.Begin);
                _fileStream.Read(page, 0, PageSize);
            }

            _pages[pageNum] = page;
            if (pageNum >= NumPages)
            {
                NumPages = pageNum + 1;
            }
        }

        return _pages[pageNum];
    }

    public void Flush(uint pageNum)
    {
        if (!_pages.ContainsKey(pageNum))
        {
            throw new Exception($"Tries to flush a non-existent page.");
        }

        _fileStream.Seek(pageNum * PageSize, SeekOrigin.Begin);
        _fileStream.Write(_pages[pageNum], 0, PageSize);
    }

    public void FlushAll()
    {
        foreach (var pageNum in _pages.Keys)
        {
            Flush(pageNum);
        }
    }

    public void Dispose()
    {
        FlushAll();

        _fileStream?.Dispose();
    }
}