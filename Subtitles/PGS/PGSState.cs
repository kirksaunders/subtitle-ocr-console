using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace subtitle_ocr_console.Subtitles.PGS;

class PGSState
{
    private class WritableImage
    {
        public Image<Rgba32> Image { get; private set; }

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

    private readonly List<CompositionObject> _compositionObjects = new();
    private readonly List<WritableImage> _objects = new();
    private readonly List<Rgba32[]> _palettes = new();
    private readonly List<WindowDefinition> _windows = new();

    private int _displayWidth;
    private int _displayHeight;
    private int _frameRate;
    private int _timestamp;
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
        _currentPalette = -1;
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

        foreach ((var color, var length) in RunLengthDecoder.Decode(segment.RLEData))
        {
            if (color >= _palettes[_currentPalette].Length)
            {
                throw new PGSStateException("Tried to write color outside palette range");
            }

            var rgba = _palettes[_currentPalette][color];
            for (var i = 0; i < length; i++)
            {
                image.WritePixel(rgba);
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
            _palettes.Add(new Rgba32[256]);
        }

        var palette = _palettes[segment.PaletteID];

        // Make all palette entries transparent
        for (var i = 0; i < palette.Length; i++)
        {
            palette[i] = new Rgba32(0, 0, 0, 0);
        }

        foreach (var entry in segment.Entries)
        {
            // Note: Since EntryID is a byte, it can't be outside range of palette array
            palette[entry.EntryID] = entry.AsRGBA();
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

            _currentPalette = segment.PaletteID;
        }

        // Calculate timestamp (in milliseconds)
        _timestamp = (int)segment.Header.PresentationTimestamp / 90;
    }

    public PGSFrame GetFrame()
    {
        var frame = new PGSFrame(_timestamp);
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
            var img = windowImages[i];
            if (img != null)
            {
                frame.AddImage(new PGSFrame.PGSImage(img, _windows[i].HorizontalPosition, _windows[i].VerticalPosition));
            }
        }

        return frame;
    }
}
