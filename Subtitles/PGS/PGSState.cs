using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using subtitle_ocr_console.Subtitles.Segmentation;

namespace subtitle_ocr_console.Subtitles.PGS;

class PGSState
{
    private class WritableImage
    {
        public Image<Rgba32> Image;

        private int _writePositionX = 0;
        private int _writePositionY = 0;

        public WritableImage(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new PGSStateException("Width and height of image must be positive nonzero");
            }

            Image = new Image<Rgba32>(width, height);
        }

        public void WritePixel(Rgba32 color)
        {
            if (_writePositionY >= Image.Height)
            {
                throw new PGSStateException("Attempted to write beyond end of image");
            }

            Image[_writePositionX, _writePositionY] = color;

            _writePositionX++;
            if (_writePositionX >= Image.Width)
            {
                _writePositionX = 0;
                _writePositionY++;
            }
        }
    }

    private List<CompositionObject> _compositionObjects = new();
    private List<WritableImage> _objects = new();
    private List<List<Rgba32>> _palettes = new();
    private List<WindowDefinition> _windows = new();

    private int _displayWidth;
    private int _displayHeight;
    private int _frameRate;
    private int _currentPalette = -1;

    public void ResetCompositionObjects()
    {
        _compositionObjects.Clear();
    }

    public void ResetObjects()
    {
        _objects.Clear();
    }

    public void ResetPalettes()
    {
        _palettes.Clear();
    }

    public void ResetWindows()
    {
        _windows.Clear();
    }

    private void DefineCompositionObject(CompositionObject compositionObject)
    {
        _compositionObjects.Add(compositionObject);
    }

    public void DefineObject(ODSegment segment)
    {
        // TODO: Determine what we should do if object doesn't already exist
        /*if (segment.ObjectID >= _objects.Count)
        {
            throw new PGSStateException("Object ID is out of bounds");
        }*/
        if (segment.ObjectID > _objects.Count)
        {
            throw new PGSStateException("Unable to define object, index skipped");
        }
        else if (segment.ObjectID == _objects.Count)
        {
            _objects.Add(new WritableImage(segment.Width, segment.Height));
        }
        else
        {
            if (segment.Last == ODSegment.LastInSequence.First || segment.Last == ODSegment.LastInSequence.Both)
            {
                _objects[segment.ObjectID] = new WritableImage(segment.Width, segment.Height);
            }
        }

        if (_currentPalette == -1)
        {
            throw new PGSStateException("Tried to write image without a palette set");
        }

        var image = _objects[segment.ObjectID];

        foreach (var pixel in segment.Pixels)
        {
            if (pixel >= _palettes[_currentPalette].Count)
            {
                // TODO: Determine whether we need to throw or write transparent pixel
                image.WritePixel(new Rgba32(0, 0, 0, 0));
            }
            else
            {
                image.WritePixel(_palettes[_currentPalette][pixel]);
            }
        }
    }

    public void DefinePalette(PDSegment segment)
    {
        // TODO: Determine what we should do if palette doesn't already exist
        /*if (segment.PaletteID >= _palettes.Count)
        {
            throw new PGSStateException("Palette ID is out of bounds");
        }*/
        while (segment.PaletteID >= _palettes.Count)
        {
            _palettes.Add(new());
        }

        var palette = _palettes[segment.PaletteID];
        palette.Clear();
        palette.EnsureCapacity(segment.Entries.Count);

        foreach (var entry in segment.Entries)
        {
            palette.Add(entry.AsRGBA());
        }
    }

    public void DefineWindows(WDSegment segment)
    {
        foreach (var window in segment.Windows)
        {
            // TODO: Determine what we should do if window doesn't already exist
            if (window.WindowID > _windows.Count)
            {
                throw new PGSStateException("Unable to define window, index skipped");
            }
            else if (window.WindowID == _windows.Count)
            {
                _windows.Add(window);
            }
            else
            {
                _windows[window.WindowID] = window;
            }
        }
    }

    public void ProcessPCS(PCSegment segment)
    {
        _displayWidth = segment.Width;
        _displayHeight = segment.Height;
        _frameRate = segment.FrameRate;
        _currentPalette = segment.PaletteID;

        ResetCompositionObjects();
        _compositionObjects.EnsureCapacity(segment.NumberObjects);
        foreach (var compositionObject in segment.CompositionObjects)
        {
            DefineCompositionObject(compositionObject);
        }

        if (segment.Type == PCSegment.CompositionType.EpochStart)
        {
            ResetObjects();
            ResetPalettes();
            ResetWindows();
        }
    }

    public void WriteResult(String name)
    {
        var windowImages = new Image<Rgba32>?[_windows.Count];

        foreach (var compositionObject in _compositionObjects)
        {
            if (compositionObject.ObjectID >= _objects.Count)
            {
                throw new PGSStateException("Composition object referenced object that does not exist");
            }

            if (compositionObject.WindowID >= _windows.Count)
            {
                throw new PGSStateException("Composition object referenced window that does not exist");
            }

            // Ensure window's image exists
            var window = _windows[compositionObject.WindowID];
            if (windowImages[compositionObject.WindowID] == null)
            {
                windowImages[compositionObject.WindowID] = new Image<Rgba32>(window.Width, window.Height);
            }

            var img = _objects[compositionObject.ObjectID].Image;
            if (compositionObject.Cropped)
            {
                img = img.Clone(ctx => ctx.Crop(new Rectangle(
                    compositionObject.CropHorizontalPosition,
                    compositionObject.CropVerticalPosition,
                    compositionObject.CropWidth,
                    compositionObject.CropHeight
                )));
            }

            int x = compositionObject.HorizontalPosition - window.HorizontalPosition;
            int y = compositionObject.VerticalPosition - window.VerticalPosition;

            windowImages[compositionObject.WindowID].Mutate(ctx =>
                ctx.DrawImage(img, new Point(x, y), 1.0f)
            );
        }

        for (int i = 0; i < windowImages.Length; i++)
        {
            if (windowImages[i] != null)
            {
                windowImages[i].Save(name + "_window_" + i.ToString() + ".png");

#pragma warning disable CS8604 // Possible null reference argument.
                var pre = new PreprocessedImage(windowImages[i], 0.5);
#pragma warning restore CS8604 // Possible null reference argument.
                pre.Process();

                var segmenter = new LineSegmenter(pre.GetImage());
                segmenter.Segment();

                int count = 0;
                foreach (var line in segmenter.Lines)
                {
                    line.Save(name + "_window_" + i.ToString() + "_line_" + count + ".png");
                    count++;
                }
            }
        }
    }
}
