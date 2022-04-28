using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace subtitle_ocr_console.Subtitles.Segmentation;

public static class LineCropper
{
    private const int Threshold = 3;

    public static Image<A8> Crop(Image<A8> line)
    {
        // Get leftmost column above threshold
        var leftmost = 0;
        for (; leftmost < line.Width; leftmost++)
        {
            int count = 0;
            for (var y = 0; y < line.Height; y++)
            {
                if (line[leftmost, y].PackedValue != 0)
                {
                    count++;
                }
            }

            if (count > Threshold)
            {
                break;
            }
        }

        // Get rightmost column above threshold
        var rightmost = line.Width - 1;
        for (; rightmost >= 0; rightmost--)
        {
            int count = 0;
            for (var y = 0; y < line.Height; y++)
            {
                if (line[rightmost, y].PackedValue != 0)
                {
                    count++;
                }
            }

            if (count > Threshold)
            {
                break;
            }
        }

        // Do crop
        var width = rightmost - leftmost + 1;
        return line.Clone(ctx =>
            ctx.Crop(new Rectangle(leftmost, 0, width, line.Height))
        );
    }
}
