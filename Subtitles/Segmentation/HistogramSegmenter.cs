namespace subtitle_ocr_console.Subtitles.Segmentation;

public static class HistogramSegmenter
{
    public static List<int> Segment(List<int> histogram, int stride, double upperThreshold, double lowerThreshold, int minSize)
    {
        var segmentPoints = new List<int>();
        bool high = false;
        for (int pos = 0; pos < histogram.Count; pos += stride)
        {
            int sum = 0;
            int count = Math.Min(stride, histogram.Count - pos);
            for (int i = 0; i < count; i++)
            {
                sum += histogram[pos + i];
            }

            double average = (double)sum / count;

            if (high && average < lowerThreshold)
            {
                int point = pos - stride + stride / 2; // Insert previous as point
                int width = point - segmentPoints[^1] + 1;
                if (width >= minSize)
                {
                    high = false;
                    segmentPoints.Add(point);
                }
            }
            else if (!high && average > upperThreshold)
            {
                high = true;
                segmentPoints.Add(pos + count / 2);
            }
        }

        // Every opening segment point needs a closing one
        if (segmentPoints.Count % 2 != 0)
        {
            segmentPoints.Add(histogram.Count - 1);
        }

        return segmentPoints;
    }
}
