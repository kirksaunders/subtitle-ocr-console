using System.IO;

namespace subtitle_ocr_console.Subtitles.PGS;

class PDSegment : Segment
{
    public byte PaletteID { get; private set; }
    public byte VersionNumber { get; private set; }
    public List<PaletteEntry> Entries { get; } = new List<PaletteEntry>();

    private PDSegment(SegmentHeader header)
        : base(header)
    {
    }

    public static PDSegment ReadFromBinary(SegmentHeader header, BinaryReader reader)
    {
        var instance = new PDSegment(header);
        instance.InitializeFromBinary(reader);

        return instance;
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        try
        {
            PaletteID = reader.ReadByte();
            VersionNumber = reader.ReadByte();

            InitializeEntries(reader);
        }
        catch (Exception ex) when (ex is not PGSReadException)
        {
            throw new PGSReadException("Internal exception when reading PD segment", ex);
        }
    }

    private void InitializeEntries(BinaryReader reader)
    {
        int bytesRead = 2;
        while ((Header.Size - bytesRead) >= 5)
        {
            var paletteEntry = PaletteEntry.ReadFromBinary(reader);
            bytesRead += 5;

            Entries.Add(paletteEntry);
        }

        if (bytesRead < Header.Size)
        {
            throw new PGSReadException("PD segment has data left over after reading palette entries");
        }
    }
}