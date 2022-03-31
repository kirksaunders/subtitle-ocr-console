using System.Collections.ObjectModel;

using SixLabors.ImageSharp;
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

    private List<Image<A8>> _lines = new();
    public ReadOnlyCollection<Image<A8>> Lines => _lines.AsReadOnly();

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

        for (int i = 0; i < segmentPoints.Count; i += 2)
        {
            var height = segmentPoints[i + 1] - segmentPoints[i] + 1;
            _lines.Add(_image.Clone(ctx =>
                ctx.Crop(new Rectangle(0, segmentPoints[i], _image.Width, height))
            ));
        }
    }

    public void Segment()
    {
        GenerateHistogram();
        GenerateImages();
    }
}
