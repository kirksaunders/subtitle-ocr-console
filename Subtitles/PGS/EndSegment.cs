using System.IO;

namespace subtitle_ocr_console.Subtitles.PGS;

class EndSegment : Segment
{
    public EndSegment(SegmentHeader header)
        : base(header)
    {
    }

    public static EndSegment ReadFromBinary(SegmentHeader header, BinaryReader reader)
    {
        var instance = new EndSegment(header);
        instance.InitializeFromBinary(reader);

        return instance;
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        // End segments don't have any body data
    }
}