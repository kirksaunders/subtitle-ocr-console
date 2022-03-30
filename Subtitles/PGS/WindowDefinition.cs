using System.IO;

namespace subtitle_ocr_console.Subtitles.PGS;

class WindowDefinition
{
    public byte WindowID { get; private set; }
    public ushort HorizontalPosition { get; private set; }
    public ushort VerticalPosition { get; private set; }
    public ushort Width { get; private set; }
    public ushort Height { get; private set; }

    private WindowDefinition()
    {
    }

    public static WindowDefinition ReadFromBinary(BinaryReader reader)
    {
        var instance = new WindowDefinition();
        instance.InitializeFromBinary(reader);

        return instance;
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        try
        {
            WindowID = reader.ReadByte();
            HorizontalPosition = reader.ReadUInt16();
            VerticalPosition = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
        }
        catch (Exception ex) when (ex is not PGSReadException)
        {
            throw new PGSReadException("Internal exception when reading window definition", ex);
        }
    }
}