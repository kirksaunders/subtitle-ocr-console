using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace subtitle_ocr_console.Subtitles.Segmentation;

public class PreprocessedImage
{
    private Image<Rgba32> _inputImage;
    private double _threshold;
    private Image<A8>? _outputImage;

    public PreprocessedImage(Image<Rgba32> inputImage, double threshold)
    {
        _inputImage = inputImage;
        _threshold = threshold;
    }

    public void Process()
    {
        byte threshold = (byte)(_threshold * 255);

        var img = _inputImage.Clone();

        // TODO: Check or ignore warning
        img.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < img.Height; y++)
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                foreach (ref Rgba32 pixel in pixelRow)
                {
                    if (pixel.A < threshold)
                    {
                        pixel = new Rgba32(0, 0, 0, 255);
                    }
                }
            }
        });

        img.Mutate(ctx =>
            ctx.BinaryThreshold(0.5f, new Color(new Rgba32(0, 0, 0, 255)), new Color(new Rgba32(0, 0, 0, 0)))
        );

        _outputImage = img.CloneAs<A8>();
    }

    public Image<A8> GetImage()
    {
        if (_outputImage == null)
        {
            throw new InvalidOperationException("Tried to retrieve image before processessing it");
        }

        return _outputImage;
    }
}
