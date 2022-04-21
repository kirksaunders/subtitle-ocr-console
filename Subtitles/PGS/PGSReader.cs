namespace subtitle_ocr_console.Subtitles.PGS;

// TODO: Rename PGSReader to something more appropriate like PGS or PGSSubtitle or something
public class PGSReader
{
    private List<Segment> _segments = new();

    public PGSReader(BinaryReader reader)
    {
        InitializeFromBinary(reader);
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        while (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            var header = new SegmentHeader(reader);

            Segment segment;
            switch (header.Type)
            {
                case SegmentHeader.SegmentType.PDS:
                    segment = new PDSegment(header, reader);
                    break;

                case SegmentHeader.SegmentType.ODS:
                    segment = new ODSegment(header, reader);
                    break;

                case SegmentHeader.SegmentType.PCS:
                    segment = new PCSegment(header, reader);
                    break;

                case SegmentHeader.SegmentType.WDS:
                    segment = new WDSegment(header, reader);
                    break;

                case SegmentHeader.SegmentType.END:
                    segment = new EndSegment(header, reader);
                    break;

                default:
                    throw new InvalidOperationException("Unreachable code");
            }

            _segments.Add(segment);
        }

        Console.WriteLine("Number of segments: " + _segments.Count);
    }

    public void WriteImages(DirectoryInfo outputDir)
    {
        var state = new PGSState();
        var count = 0;

        for (int i = 0; i < _segments.Count; i++)
        {
            var segment = _segments[i];

            switch (segment.Header.Type)
            {
                case SegmentHeader.SegmentType.PDS:
                    state.DefinePalette((PDSegment)segment);
                    break;

                case SegmentHeader.SegmentType.ODS:
                    state.DefineObject((ODSegment)segment);
                    break;

                case SegmentHeader.SegmentType.PCS:
                    state.ProcessPCS((PCSegment)segment);
                    break;

                case SegmentHeader.SegmentType.WDS:
                    state.DefineWindows((WDSegment)segment);
                    break;

                case SegmentHeader.SegmentType.END:
                    state.WriteResult(outputDir + "/" + count.ToString());
                    count++;
                    break;

                default:
                    break;
            }
        }
    }
}
