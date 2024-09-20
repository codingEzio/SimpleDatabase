using SimpleDatabase.Enum;
using SimpleDatabase.Model;

namespace SimpleDatabase.Utility;

public class Table
{
    public Pager Pager { get; set; }
    public uint RootPageNum { get; set; }

    public Table(string filename)
    {
        Pager = new Pager(filename);
        RootPageNum = 0;

        if (Pager.NumPages == 0)
        {
            BTreeLeafNode rootNode = new BTreeLeafNode
            {
                Type = NodeType.Leaf,
                IsRoot = true,
                NumCells = 0,
                NextLeaf = 0
            };

            byte[] rootPage = SerializeNode(rootNode);
            Array.Copy(rootPage, Pager.GetPage(0), rootPage.Length);
        }
    }

    private byte[] SerializeNode(BTreeNode node)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
        {
            binaryWriter.Write((byte)node.Type);
            binaryWriter.Write(node.IsRoot);
            binaryWriter.Write(node.ParentPointer);

            if (node is BTreeLeafNode leafNode)
            {
                binaryWriter.Write(leafNode.NumCells);
                binaryWriter.Write(leafNode.NextLeaf);

                foreach (var cell in leafNode.Cells.OrderBy(c => c.Key))
                {
                    binaryWriter.Write(cell.Key);
                    binaryWriter.Write(cell.Value.Serialize());
                }
            } else if (node is BTreeInternalNode internalNode)
            {
                binaryWriter.Write(internalNode.NumKeys);
                binaryWriter.Write(internalNode.RightChild);

                for (uint i = 0; i < internalNode.NumKeys; i++)
                {
                    binaryWriter.Write(internalNode.Children[i]);
                    binaryWriter.Write(internalNode.Keys[i]);
                }
            }

            return memoryStream.ToArray();
        }
    }

    private BTreeNode DeserializeNode(byte[] data)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        using (BinaryReader binaryReader = new BinaryReader(memoryStream))
        {
            NodeType type = (NodeType)binaryReader.ReadByte();

            bool isRoot = binaryReader.ReadBoolean();
            uint parentPointer = binaryReader.ReadUInt32();

            if (type == NodeType.Leaf)
            {
                BTreeLeafNode leafNode = new BTreeLeafNode()
                {
                    Type = type,
                    IsRoot = isRoot,
                    ParentPointer = parentPointer,
                    NumCells = binaryReader.ReadUInt32(),
                    NextLeaf = binaryReader.ReadUInt32()
                };

                for (uint i = 0; i < leafNode.NumCells; i++)
                {
                    uint key = binaryReader.ReadUInt32();
                    byte[] rowData = binaryReader.ReadBytes(Row.UsernameSize + Row.EmailSize + 4);

                    leafNode.Cells[key] = Row.Deserialize(rowData);
                }

                return leafNode;
            }
            else
            {
                BTreeInternalNode internalNode = new BTreeInternalNode()
                {
                    Type = type,
                    IsRoot = isRoot,
                    ParentPointer = parentPointer,
                    NumKeys = binaryReader.ReadUInt32(),
                    RightChild = binaryReader.ReadUInt32()
                };

                for (uint i = 0; i < internalNode.NumKeys; i++)
                {
                    internalNode.Children[i] = binaryReader.ReadUInt32();
                    internalNode.Keys[i] = binaryReader.ReadUInt32();
                }

                return internalNode;
            }
        }
    }
}