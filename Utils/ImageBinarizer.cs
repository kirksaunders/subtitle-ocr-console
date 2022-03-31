using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace subtitle_ocr_console.Utils;

public static class ImageBinarizer
{
    public static Image<A8> Binarize(Image<Rgba32> inputImage, double threshold)
    {
        byte thresholdByte = (byte)(threshold * 255);

        var img = inputImage.Clone();
        img.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < img.Height; y++)
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                foreach (ref Rgba32 pixel in pixelRow)
                {
                    if (pixel.A < thresholdByte)
                    {
                        pixel = new Rgba32(0, 0, 0, 255);
                    }
                }
            }
        });
        img.Mutate(ctx =>
            ctx.BinaryThreshold(0.5f, new Color(new Rgba32(0, 0, 0, 255)), new Color(new Rgba32(0, 0, 0, 0)))
        );

        return img.CloneAs<A8>();
    }
}
