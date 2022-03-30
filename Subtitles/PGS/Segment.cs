namespace subtitle_ocr_console.Subtitles.PGS;

abstract class Segment
{
    public SegmentHeader Header { get; private set; }

    public Segment(SegmentHeader header)
    {
        Header = header;
    }
}