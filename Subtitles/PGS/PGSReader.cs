namespace subtitle_ocr_console.Subtitles.PGS;

// TODO: Rename PGSReader to something more appropriate like PGS or PGSSubtitle or something
public class PGSReader
{
    private readonly List<Segment> _segments = new();

    public PGSReader(BinaryReader reader)
    {
        InitializeFromBinary(reader);
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        while (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            var header = new SegmentHeader(reader);
            Segment segment = header.Type switch
            {
                SegmentHeader.SegmentType.PDS => new PDSegment(header, reader),
                SegmentHeader.SegmentType.ODS => new ODSegment(header, reader),
                SegmentHeader.SegmentType.PCS => new PCSegment(header, reader),
                SegmentHeader.SegmentType.WDS => new WDSegment(header, reader),
                SegmentHeader.SegmentType.END => new EndSegment(header, reader),
                _ => throw new InvalidOperationException("Unreachable code"),
            };

            _segments.Add(segment);
        }
    }

    public IEnumerable<PGSFrame> GetFrames()
    {
        var state = new PGSState();

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
                    yield return state.GetFrame();
                    break;

                default:
                    break;
            }
        }
    }

    public IEnumerable<(PGSFrame, double)> GetFramesWithProgress()
    {
        var state = new PGSState();

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
                    yield return (state.GetFrame(), (double)i / (_segments.Count - 1));
                    break;

                default:
                    break;
            }
        }
    }
}
