namespace subtitle_ocr_console.Subtitles.PGS;

class EndSegment : Segment
{
    public EndSegment(SegmentHeader header, BinaryReader reader)
        : base(header)
    {
        InitializeFromBinary(reader);
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        // End segments don't have any body data
    }
}