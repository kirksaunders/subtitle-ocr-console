namespace subtitle_ocr_console.Subtitles.PGS;

class SegmentHeader
{
    public enum SegmentType
    {
        PDS, // Palette Definition Segment
        ODS, // Object Definition Segment
        PCS, // Presentation Composition Segment
        WDS, // Window Definition Segment
        END  // End of Display Set Segment
    }

    public uint PresentationTimestamp { get; private set; }
    public uint DecodingTimestamp { get; private set; }
    public SegmentType Type { get; private set; }
    public ushort Size { get; private set; }

    public SegmentHeader(BinaryReader reader)
    {
        InitializeFromBinary(reader);
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        try
        {
            ushort magicNumber = reader.ReadUInt16();

            if (magicNumber != 0x5047)
            {
                throw new PGSReadException("Segment header magic number is not correct");
            }

            PresentationTimestamp = reader.ReadUInt32();
            DecodingTimestamp = reader.ReadUInt32();

            byte type = reader.ReadByte();
            Type = type switch
            {
                0x14 => SegmentType.PDS,
                0x15 => SegmentType.ODS,
                0x16 => SegmentType.PCS,
                0x17 => SegmentType.WDS,
                0x80 => SegmentType.END,
                _ => throw new PGSReadException($"Unknown segment type: {type}"),
            };
            Size = reader.ReadUInt16();

            if (Type == SegmentType.END && Size != 0)
            {
                throw new PGSReadException($"End segment should have size 0, has size: {Size}");
            }
        }
        catch (Exception ex) when (ex is not PGSReadException)
        {
            throw new PGSReadException("Internal exception when reading segment header", ex);
        }
    }
}