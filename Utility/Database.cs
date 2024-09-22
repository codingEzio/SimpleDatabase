using SimpleDatabase.Model;

namespace SimpleDatabase.Utility;

public class Database : IDisposable
{
    private Table _table;

    public Database(string filename)
    {
        _table = new Table(filename);
    }

    public void Insert(uint id, string username, string email)
    {
        if (username.Length > Row.UsernameSize || email.Length > Row.EmailSize)
        {
            throw new ArgumentException("Username or email exceeds maximum allowed length.");
        }

        Row row = new Row
        {
            Id = id,
            Username = username,
            Email = email
        };
        _table.Insert(id, row);
    }

    public IEnumerable<Row> Select()
    {
        return _table.Select();
    }

    public void PrintConstants()
    {
        Console.WriteLine($"ROW_SIZE:\t{Row.UsernameSize + Row.EmailSize + 4}");
        Console.WriteLine($"COMMON_NODE_HEADER_SIZE:\t{1 + 1 + 4}");
        Console.WriteLine($"LEAF_NODE_HEADER_SIZE:\t{1 + 1 + 4 + 4 + 4}");
        Console.WriteLine($"LEAF_NODE_MAX_CELLS:\t{4 + Row.UsernameSize + Row.EmailSize + 4}");
        Console.WriteLine($"LEAF_NODE_SPACE_FOR_CELLS:\t{Pager.PageSize - (1 + 1 + 4 + 4 + 4)}");
        Console.WriteLine($"LEAF_NODE_MAX_CELLS:\t{BTreeLeafNode.LeafNodeMaxCells}");
        Console.WriteLine($"INTERNAL_NODE_MAX_CELLS:\t{BTreeInternalNode.InternalNodeMaxCells}");
    }

    public void PrintTree()
    {
        // #TODO
        Console.WriteLine("BTree printing functionality not implemented yet.");
    }

    public void Dispose()
    {
        // Resources cleanup handled by Pager (it got a Dispose as well)
    }
}