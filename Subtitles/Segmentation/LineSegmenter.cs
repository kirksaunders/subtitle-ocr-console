using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace subtitle_ocr_console.Subtitles.Segmentation;

public static class LineSegmenter
{
    private const int Stride = 3;
    private const double UpperThreshold = 3.0;
    private const double LowerThreshold = 2.0;
    private const int MinHeight = 15;

    public static List<Image<A8>> Segment(Image<A8> image)
    {
        var histogram = GenerateHistogram(image);
        var segmentPoints = HistogramSegmenter.Segment(histogram, Stride, UpperThreshold, LowerThreshold, MinHeight);

        var lines = new List<Image<A8>>();
        for (int i = 0; i < segmentPoints.Count; i += 2)
        {
            var height = segmentPoints[i + 1] - segmentPoints[i] + 1;
            var line = image.Clone(ctx =>
                ctx.Crop(new Rectangle(0, segmentPoints[i], image.Width, height))
            );
            line = LineCropper.Crop(line);
            lines.Add(line);
        }

        return lines;
    }

    private static List<int> GenerateHistogram(Image<A8> image)
    {
        var histogram = new List<int>(image.Height);

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < image.Height; y++)
            {
                int count = 0;
                Span<A8> pixelRow = accessor.GetRowSpan(y);

                foreach (ref A8 pixel in pixelRow)
                {
                    if (pixel.PackedValue != 0)
                    {
                        count++;
                    }
                }

                histogram.Add(count);
            }
        });

        return histogram;
    }
}
