namespace SimpleDatabase.Model;

#region using

using SimpleDatabase.Enum;

#endregion

/*
                      | 50 | 100| 150|   |  <--- 根节点 (也是内部节点)
                      +---+---+---+---+
                     /    |    |    \
                    /     |    |     \
                   /      |    |      \
                  /       |    |       \
                 v       v    v       v
               +---+---+ +---+---+ +---+---+
               | 10| 20| | 60| 80| |120| 140|  <--- 内部节点
               +---+---+ +---+---+ +---+---+
             /   |   |  \ /  |   |  \ /  |   |  \
            /    |   |   \/   |   |   \/   |   |   \
           /     |   |    v    |   |     v    |   |    \
          v      v   v       v   v        v    v       v
       +---+---+ +---+---+ +---+---+ +---+---+ +---+---+
       | 5 | 8 | |15|18| |55|58| |65|70| |115|118| |125|130|  <--- 内部节点
       +---+---+ +---+---+ +---+---+ +---+---+ +---+---+
      / |  |  \ / | |  \ / | |  \ / | |  \ / | |  \ / | |  \
     v  v  v  v v  v  v v  v  v v  v  v v  v  v v  v  v v  v  v
   +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+
   | 0 | | 3 | | 4 | | 7 | | 9 | |11| |13| |16| |19| |21| |24| <--- 叶子节点
   +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+ +---+
 */

/// <summary>
///  Base class for all nodes in a B-Tree
/// </summary>
public abstract class BTreeNode
{
    public NodeType Type { get; set; }
    public bool IsRoot { get; set; }
    public uint ParentPointer { get; set; }
}

/// <summary>
///  如果一个节点存储了实际的数据记录，那么它就是叶子节点。
/// </summary>
public class BTreeLeafNode : BTreeNode
{
    public const int LeafNodeMaxCells = 13;
    public uint NumCells { get; set; }
    public uint NextLeaf { get; set; }
    public Dictionary<uint, Row> Cells { get; set; } = new Dictionary<uint, Row>();
}

/// <summary>
///  如果一个节点只包含键，并用这些键来划分更小的搜索范围，指向下一层节点，那么它就是一个内部节点，无论它在 B 树的哪一层。
/// </summary>
public class BTreeInternalNode : BTreeNode
{
    public const int InternalNodeMaxCells = 3;
    public uint NumKeys { get; set; }
    public uint RightChild { get; set; }
    public Dictionary<uint, uint> Children { get; set; } = new Dictionary<uint, uint>();
    public Dictionary<uint, uint> Keys { get; set; } = new Dictionary<uint, uint>();
}
