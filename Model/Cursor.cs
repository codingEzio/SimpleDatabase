using SimpleDatabase.Utility;

namespace SimpleDatabase.Model;

public class Cursor
{
    public Table Table { get; set; }
    public uint PageNum { get; set; }
    public uint CellNum { get; set; }
    public bool EndOfTable { get; set; }


}