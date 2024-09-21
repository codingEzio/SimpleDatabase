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

    public IEnumerable<Row> Select()
    {
        uint pageNum = RootPageNum;
        BTreeNode node;

        do
        {
            node = DeserializeNode(Pager.GetPage(pageNum));

            if (node.Type == NodeType.Leaf)
            {
                BTreeLeafNode leafNode = (BTreeLeafNode)node;
                foreach (var cell in leafNode.Cells.OrderBy(c => c.Key))
                {
                    yield return cell.Value;
                }

                pageNum = leafNode.NextLeaf;
            }
            else
            {
                BTreeInternalNode internalNode = (BTreeInternalNode)node;

                pageNum = internalNode.Children[0];
            }
        } while (pageNum != 0);
    }

    public Cursor Find(uint key)
    {
        uint pageNum = RootPageNum;
        BTreeNode node = DeserializeNode(Pager.GetPage(pageNum));


        while (node.Type == NodeType.Internal)
        {
            BTreeInternalNode internalNode = (BTreeInternalNode)node;
            uint childIndex = 0;

            while (childIndex < internalNode.NumKeys && key >= internalNode.Keys[childIndex])
            {
                childIndex += 1;
            }

            pageNum = childIndex == internalNode.NumKeys
                ? internalNode.RightChild
                : internalNode.Children[childIndex];
        }

        BTreeLeafNode leafNode = (BTreeLeafNode)node;

        return new Cursor()
        {
            Table = this,
            PageNum = pageNum,
            CellNum = 0,
            EndOfTable = leafNode.NumCells == 0
        };
    }

    public void Insert(uint key, Row value)
    {
        Cursor cursor = Find(key);
        BTreeLeafNode node = (BTreeLeafNode)DeserializeNode(Pager.GetPage(cursor.PageNum));

        if (node.Cells.ContainsKey(key))
        {
            throw new Exception($"Key {key} already exists in the table.");
        }

        if (node.NumCells >= BTreeLeafNode.LeafNodeMaxCells)
        {
            SplitAndInsert(cursor, key, value);
        }
        else
        {
            node.Cells[key] = value;
            node.NumCells += 1;

            byte[] serializedNode = SerializeNode(node);
            Array.Copy(serializedNode, Pager.GetPage(cursor.PageNum), serializedNode.Length);

            Pager.Flush(cursor.PageNum);
        }
    }

    private void SplitAndInsert(Cursor cursor, uint key, Row value)
    {
        BTreeLeafNode oldNode = (BTreeLeafNode)DeserializeNode(Pager.GetPage(cursor.PageNum));
        uint newPageNum = Pager.NumPages;
        BTreeLeafNode newNode = new BTreeLeafNode()
        {
            Type = NodeType.Leaf,
            IsRoot = false,
            NumCells = 0,
            NextLeaf = oldNode.NextLeaf,
            ParentPointer = oldNode.ParentPointer
        };

        oldNode.NextLeaf = newPageNum;

        int splitIndex = BTreeLeafNode.LeafNodeMaxCells / 2;
        var sortedCells = oldNode.Cells.OrderBy(c => c.Key).ToList();

        for (int i = splitIndex; i < sortedCells.Count; i++)
        {
            var cell = sortedCells[i];

            newNode.Cells[cell.Key] = cell.Value;
            newNode.NumCells += 1;

            oldNode.Cells.Remove(cell.Key);
        }

        oldNode.NumCells = (uint)splitIndex;

        if (key < newNode.Cells.Keys.First())
        {
            oldNode.Cells[key] = value;
            oldNode.NumCells += 1;
        }
        else
        {
            newNode.Cells[key] = value;
            newNode.NumCells += 1;
        }

        byte[] oldNodeSerialized = SerializeNode(oldNode);
        byte[] newNodeSerialized = SerializeNode(newNode);
        Array.Copy(oldNodeSerialized, Pager.GetPage(cursor.PageNum), oldNodeSerialized.Length);
        Array.Copy(newNodeSerialized, Pager.GetPage(newPageNum), newNodeSerialized.Length);
        Pager.Flush(cursor.PageNum);
        Pager.Flush(newPageNum);

        if (oldNode.IsRoot)
        {
            CreateNewRoot(newPageNum);
        }
        else
        {
            uint parentPageNum = oldNode.ParentPointer;
            BTreeInternalNode parent = (BTreeInternalNode)DeserializeNode(Pager.GetPage(parentPageNum));

            uint newMaxKey = oldNode.Cells.Keys.Max();

            UpdateInternalNodeKey(parent, cursor.PageNum, newMaxKey);
            InsertInternalNode(parent, newPageNum, newNode.Cells.Keys.Max());
        }
    }

    private void CreateNewRoot(uint rightChildPageNum)
    {
        BTreeLeafNode rightChild = (BTreeLeafNode)DeserializeNode(Pager.GetPage(rightChildPageNum));
        BTreeLeafNode leftChild = (BTreeLeafNode)DeserializeNode(Pager.GetPage(RootPageNum));

        uint newRootPageNum = Pager.NumPages;
        BTreeInternalNode rootNode = new BTreeInternalNode()
        {
            Type = NodeType.Internal,
            IsRoot = true,
            NumKeys = 1,
            RightChild = rightChildPageNum,
            ParentPointer = 0
        };

        rootNode.Children[0] = RootPageNum;
        rootNode.Keys[0] = leftChild.Cells.Keys.Max();

        leftChild.ParentPointer = newRootPageNum;
        rightChild.ParentPointer = newRootPageNum;

        byte[] rootSerialized = SerializeNode(rootNode);
        byte[] leftChildSerialized = SerializeNode(leftChild);
        byte[] rightChildSerialized = SerializeNode(rightChild);

        Array.Copy(rootSerialized, Pager.GetPage(RootPageNum), rootSerialized.Length);
        Array.Copy(leftChildSerialized, Pager.GetPage(RootPageNum), leftChildSerialized.Length);
        Array.Copy(rightChildSerialized, Pager.GetPage(rightChildPageNum), rightChildSerialized.Length);

        RootPageNum = newRootPageNum;
    }

    private void UpdateInternalNodeKey(BTreeInternalNode node, uint oldKey, uint newKey)
    {
        var keysArray = node.Keys.Values.ToArray();

        int index = Array.IndexOf(keysArray, oldKey);
        if (index != -1)
        {
            node.Keys[(uint)index] = newKey;
        }
    }

    private void InsertInternalNode(BTreeInternalNode node, uint childPageNum, uint childMaxKey)
    {
        if (node.NumKeys >= BTreeInternalNode.InternalNodeMaxCells)
        {
            SplitInternalNode(node, childPageNum, childMaxKey);

            return;
        }

        uint newKeyIndex = node.NumKeys;
        for (uint i = 0; i < node.NumKeys; i++)
        {
            if (childMaxKey < node.Keys[i])
            {
                newKeyIndex = i;

                break;
            }
        }

        for (uint i = node.NumKeys; i > newKeyIndex; i--)
        {
            node.Children[i] = node.Children[i - 1];
            node.Keys[i] = node.Keys[i - 1];
        }

        node.Children[newKeyIndex] = childPageNum;
        node.Keys[newKeyIndex] = childMaxKey;
        node.NumKeys += 1;

        byte[] nodeSerialized = SerializeNode(node);
        Array.Copy(nodeSerialized, Pager.GetPage(node.ParentPointer), nodeSerialized.Length);

        Pager.Flush(node.ParentPointer);
    }

    private void SplitInternalNode(BTreeInternalNode oldNode, uint childPageNum, uint childMaxKey)
    {
        uint newPageNum = Pager.NumPages;
        BTreeInternalNode newNode = new BTreeInternalNode()
        {
            Type = NodeType.Internal,
            IsRoot = false,
            NumKeys = 0,
            RightChild = oldNode.RightChild,
            ParentPointer = oldNode.ParentPointer
        };

        uint splitIndex = BTreeInternalNode.InternalNodeMaxCells / 2;
        for (uint i = splitIndex; i < BTreeInternalNode.InternalNodeMaxCells; i++)
        {
            newNode.Children[newNode.NumKeys] = oldNode.Children[i];
            newNode.Keys[newNode.NumKeys] = oldNode.Keys[i];
            newNode.NumKeys += 1;

            oldNode.Children.Remove(i);
            oldNode.Keys.Remove(i);
            oldNode.NumKeys += 1;
        }

        oldNode.RightChild = newNode.Children[0];

        newNode.Children.Remove(0);
        newNode.NumKeys -= 1;

        if (childMaxKey < newNode.Keys.First().Value)
        {
            InsertInternalNode(oldNode, childPageNum, childMaxKey);
        }
        else
        {
            InsertInternalNode(newNode, childPageNum, childMaxKey);
        }

        byte[] oldNodeSerialized = SerializeNode(oldNode);
        byte[] newNodeSerialized = SerializeNode(newNode);

        Array.Copy(oldNodeSerialized, Pager.GetPage(oldNode.ParentPointer), oldNodeSerialized.Length);
        Array.Copy(newNodeSerialized, Pager.GetPage(newPageNum), newNodeSerialized.Length);

        Pager.Flush(oldNode.ParentPointer);
        Pager.Flush(newPageNum);

        if (oldNode.IsRoot)
        {
            CreateNewRoot(newPageNum);
        }
        else
        {
            uint parentPageNum = oldNode.ParentPointer;

            BTreeInternalNode parent = (BTreeInternalNode)DeserializeNode(Pager.GetPage(parentPageNum));
            UpdateInternalNodeKey(parent, oldNode.Keys.Values.Max(), oldNode.Keys.Values.Max());
            InsertInternalNode(parent, newPageNum, newNode.Keys.Values.Max());
        }
    }
}
