using System.Collections.ObjectModel;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace subtitle_ocr_console.Subtitles.PGS;

public class PGSFrame
{
    public class PGSImage
    {
        public Image<Rgba32> Img { get; private set; }
        public int XPos { get; private set; }
        public int YPos { get; private set; }

        public PGSImage(Image<Rgba32> img, int xPos, int yPos)
        {
            Img = img;
            XPos = xPos;
            YPos = yPos;
        }
    }

    // Timestamp is in milliseconds
    public int Timestamp { get; private set; }
    public List<PGSImage> _images;
    public ReadOnlyCollection<PGSImage> Images { get { return _images.AsReadOnly(); } }

    private static readonly IComparer<PGSImage> _imageComparer = Comparer<PGSImage>.Create((x, y) => x.YPos.CompareTo(y.YPos));

    public PGSFrame()
    {
        _images = new();
    }

    public PGSFrame(int timestamp)
    {
        Timestamp = timestamp;
        _images = new();
    }

    public PGSFrame(int timestamp, List<PGSImage> images)
    {
        Timestamp = timestamp;

        // Ensure images are sorted by YPos
        _images = new(images); // Make copy
        _images.Sort(_imageComparer);
    }

    public void AddImage(PGSImage image)
    {
        // Insert sorted by YPos
        int pos = _images.BinarySearch(image, _imageComparer);
        if (pos < 0)
        {
            pos = ~pos;
        }
        _images.Insert(pos, image);
    }
}