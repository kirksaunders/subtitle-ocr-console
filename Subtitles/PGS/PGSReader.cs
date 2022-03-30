namespace subtitle_ocr_console.Subtitles.PGS;

// TODO: Rename PGSReader to something more appropriate like PGS or PGSSubtitle or something
public class PGSReader
{
    private List<Segment> _segments = new();

    private PGSReader()
    {
    }

    public static PGSReader ReadFromBinary(BinaryReader reader)
    {
        var instance = new PGSReader();
        instance.InitializeFromBinary(reader);

        return instance;
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        while (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            var header = SegmentHeader.ReadFromBinary(reader);

            Segment segment;
            switch (header.Type)
            {
                case SegmentHeader.SegmentType.PDS:
                    segment = PDSegment.ReadFromBinary(header, reader);
                    break;

                case SegmentHeader.SegmentType.ODS:
                    segment = ODSegment.ReadFromBinary(header, reader);
                    break;

                case SegmentHeader.SegmentType.PCS:
                    segment = PCSegment.ReadFromBinary(header, reader);
                    break;

                case SegmentHeader.SegmentType.WDS:
                    segment = WDSegment.ReadFromBinary(header, reader);
                    break;

                case SegmentHeader.SegmentType.END:
                    segment = EndSegment.ReadFromBinary(header, reader);
                    break;

                default:
                    throw new InvalidOperationException("Unreachable code");
            }

            _segments.Add(segment);
        }

        Console.WriteLine("Number of segments: " + _segments.Count);
        Test();
    }

    public void Test()
    {
        var state = new PGSState();
        var count = 0;

        for (int i = 0; i < _segments.Count; i++)
        {
            var segment = _segments[i];

            bool quit = false;
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
                    state.WriteResult("out/" + count.ToString());
                    count++;
                    if (count >= 1000000)
                    {
                        quit = true;
                    }
                    break;

                default:
                    break;
            }

            if (quit)
            {
                break;
            }
        }
    }
}
