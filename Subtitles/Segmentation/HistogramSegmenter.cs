namespace subtitle_ocr_console.Subtitles.Segmentation;

public class HistogramSegmenter
{
    private int _stride;
    private double _upperThreshold;
    private double _lowerThreshold;
    private int _minWidth;
    private List<int> _histogram;

    public List<int> SegmentPoints = new();

    // TODO: Rename minWidth to something more accurate. Maybe minSize?
    public HistogramSegmenter(List<int> histogram, int stride, double upperThreshold, double lowerThreshold, int minWidth)
    {
        _histogram = histogram;
        _stride = stride;
        _upperThreshold = upperThreshold;
        _lowerThreshold = lowerThreshold;
        _minWidth = minWidth;
    }

    public void GenerateSegments()
    {
        bool high = false;
        for (int pos = 0; pos < _histogram.Count; pos += _stride)
        {
            int sum = 0;
            int count = Math.Min(_stride, _histogram.Count - pos);
            for (int i = 0; i < count; i++)
            {
                sum += _histogram[pos + i];
            }

            double average = (double)sum / count;

            if (high && average < _lowerThreshold)
            {
                int width = (pos + count / 2) - SegmentPoints[^1] + 1;
                if (width >= _minWidth)
                {
                    high = false;
                    SegmentPoints.Add(pos + count / 2);
                }
            }
            else if (!high && average > _upperThreshold)
            {
                high = true;
                SegmentPoints.Add(pos > 0 ? (pos - _stride) + _stride / 2 : 0);
            }
        }

        // Every opening segment point needs a closing one
        if (SegmentPoints.Count % 2 != 0)
        {
            SegmentPoints.Add(_histogram.Count - 1);
        }
    }
}
