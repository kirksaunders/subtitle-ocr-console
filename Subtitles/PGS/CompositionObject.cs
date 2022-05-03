namespace subtitle_ocr_console.Subtitles.PGS;

class CompositionObject
{
    public ushort ObjectID { get; private set; }
    public byte WindowID { get; private set; }
    public bool Cropped { get; private set; }
    public ushort HorizontalPosition { get; private set; }
    public ushort VerticalPosition { get; private set; }
    public ushort CropHorizontalPosition { get; private set; }
    public ushort CropVerticalPosition { get; private set; }
    public ushort CropWidth { get; private set; }
    public ushort CropHeight { get; private set; }

    public CompositionObject(BinaryReader reader)
    {
        InitializeFromBinary(reader);
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        try
        {
            ObjectID = reader.ReadUInt16();
            WindowID = reader.ReadByte();

            byte flag = reader.ReadByte();

            Cropped = flag switch
            {
                0x00 => false,
                0x80 => true,
                _ => throw new PGSReadException($"Unknown object cropped flag: {flag}"),
            };
            HorizontalPosition = reader.ReadUInt16();
            VerticalPosition = reader.ReadUInt16();

            if (Cropped)
            {
                CropHorizontalPosition = reader.ReadUInt16();
                CropVerticalPosition = reader.ReadUInt16();
                CropWidth = reader.ReadUInt16();
                CropHeight = reader.ReadUInt16();
            }
        }
        catch (Exception ex) when (ex is not PGSReadException)
        {
            throw new PGSReadException("Internal exception when reading composition object", ex);
        }
    }
}