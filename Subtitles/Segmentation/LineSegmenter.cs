using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace subtitle_ocr_console.Subtitles.Segmentation;

public class LineSegmenter
{
    private const int Stride = 3;
    private const double UpperThreshold = 3.0;
    private const double LowerThreshold = 2.0;
    private const int MinWidth = 15;

    private Image<A8> _image;
    private List<int> _histogram = new();

    public List<Image<A8>> Lines = new();

    public LineSegmenter(Image<A8> image)
    {
        _image = image;
    }

    private void GenerateHistogram()
    {
        _histogram.EnsureCapacity(_image.Height);

        _image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < _image.Height; y++)
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

                _histogram.Add(count);
            }
        });
    }

    public void GenerateImages()
    {
        var segmenter = new HistogramSegmenter(_histogram, Stride, UpperThreshold, LowerThreshold, MinWidth);
        segmenter.GenerateSegments();
        var segmentPoints = segmenter.SegmentPoints;

        /*var img = _image.Clone();

        for (int i = 0; i < segmentPoints.Count; i++)
        {
            var points = new PointF[2];
            points[0] = new PointF(0.0f, segmentPoints[i]);
            points[1] = new PointF((float)_image.Width - 1, segmentPoints[i]);
            img.Mutate(ctx =>
                ctx.DrawLines(new Color(new Rgba32(0, 0, 0, 255)), 2.0f, points)
            );
        }

        img.Save("out/segments.png");*/

        for (int i = 0; i < segmentPoints.Count; i += 2)
        {
            var height = segmentPoints[i + 1] - segmentPoints[i] + 1;
            Lines.Add(_image.Clone(ctx =>
                ctx.Crop(new Rectangle(0, segmentPoints[i], _image.Width, height))
            ));
            //Lines[^1].Save("out/line " + i.ToString() + ".png");
        }
    }

    public void Segment()
    {
        GenerateHistogram();
        GenerateImages();
    }
}
