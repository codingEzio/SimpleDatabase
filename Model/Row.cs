#region using

using System.Text;

#endregion

public class Row
{
    public uint Id { get; set; }

    // #TODO Hardcoded for now
    public string Username { get; set; }

    // #TODO Hardcoded for now
    public string Email { get; set; }

    public const int UsernameSize = 32;
    public const int EmailSize = 255;

    public byte[] Serialize()
    {
        using (MemoryStream memoryStream = new MemoryStream())
        using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
        {
            binaryWriter.Write(Id);
            binaryWriter.Write(Encoding.ASCII.GetBytes(Username.PadRight(UsernameSize, '\0')));
            binaryWriter.Write(Encoding.ASCII.GetBytes(Email.PadRight(EmailSize, '\0')));

            return memoryStream.ToArray();
        }
    }

    public static Row Deserialize(byte[] data)
    {
        using (MemoryStream memoryStream = new MemoryStream(data))
        using (BinaryReader binaryReader = new BinaryReader(memoryStream))
        {
            return new Row
            {
                Id = binaryReader.ReadUInt32(),
                Username = Encoding.ASCII.GetString(binaryReader.ReadBytes(UsernameSize)).Trim('\0'),
                Email = Encoding.ASCII.GetString(binaryReader.ReadBytes(EmailSize)).Trim('\0')
            };
        }
    }
}