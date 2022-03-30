using System.IO;

namespace subtitle_ocr_console.Subtitles.PGS;

class PCSegment : Segment
{
    public enum CompositionType
    {
        Normal,
        AcquisitionPoint,
        EpochStart
    }

    public ushort Width { get; private set; }
    public ushort Height { get; private set; }
    public byte FrameRate { get; private set; }
    public ushort CompositionNumber { get; private set; }
    public CompositionType Type { get; private set; }
    public bool PaletteUpdate { get; private set; }
    public byte PaletteID { get; private set; }
    public byte NumberObjects { get; private set; }
    public List<CompositionObject> CompositionObjects { get; } = new List<CompositionObject>();

    private PCSegment(SegmentHeader header)
        : base(header)
    {
    }

    public static PCSegment ReadFromBinary(SegmentHeader header, BinaryReader reader)
    {
        var instance = new PCSegment(header);
        instance.InitializeFromBinary(reader);

        return instance;
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        try
        {
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            FrameRate = reader.ReadByte();
            CompositionNumber = reader.ReadUInt16();

            byte type = reader.ReadByte();

            switch (type)
            {
                case 0x00:
                    Type = CompositionType.Normal;
                    break;

                case 0x40:
                    Type = CompositionType.AcquisitionPoint;
                    break;

                case 0x80:
                    Type = CompositionType.EpochStart;
                    break;

                default:
                    throw new PGSReadException($"Unknown composition type: {type}");
            }

            byte flag = reader.ReadByte();

            switch (flag)
            {
                case 0x00:
                    PaletteUpdate = false;
                    break;

                case 0x80:
                    PaletteUpdate = true;
                    break;

                default:
                    throw new PGSReadException($"Unknown palette update flag: {flag}");
            }

            PaletteID = reader.ReadByte();
            NumberObjects = reader.ReadByte();

            InitializeObjects(reader);
        }
        catch (Exception ex) when (ex is not PGSReadException)
        {
            throw new PGSReadException("Internal exception when reading PC segment", ex);
        }
    }

    private void InitializeObjects(BinaryReader reader)
    {
        int bytesRead = 11;
        for (int i = 0; i < NumberObjects; i++)
        {
            var compositionObject = CompositionObject.ReadFromBinary(reader);
            bytesRead += 16;

            CompositionObjects.Add(compositionObject);
        }

        if (bytesRead < Header.Size)
        {
            throw new PGSReadException("PC segment has data left over after reading composition objects");
        }
    }
}