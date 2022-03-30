using System.IO;

namespace subtitle_ocr_console.Subtitles.PGS;

class WDSegment : Segment
{
    public byte NumberWindows { get; private set; }
    public List<WindowDefinition> Windows { get; } = new List<WindowDefinition>();

    private WDSegment(SegmentHeader header)
        : base(header)
    {
    }

    public static WDSegment ReadFromBinary(SegmentHeader header, BinaryReader reader)
    {
        var instance = new WDSegment(header);
        instance.InitializeFromBinary(reader);

        return instance;
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        try
        {
            NumberWindows = reader.ReadByte();

            InitializeWindows(reader);
        }
        catch (Exception ex) when (ex is not PGSReadException)
        {
            throw new PGSReadException("Internal exception when reading PC segment", ex);
        }
    }

    private void InitializeWindows(BinaryReader reader)
    {
        int bytesRead = 1;
        for (int i = 0; i < NumberWindows; i++)
        {
            var windowDefinition = WindowDefinition.ReadFromBinary(reader);
            bytesRead += 9;

            Windows.Add(windowDefinition);
        }

        if (bytesRead < Header.Size)
        {
            throw new PGSReadException("WD segment has data left over after reading window definitions");
        }
    }
}