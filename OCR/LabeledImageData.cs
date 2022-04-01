using System.Text;
using System.Text.Json;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using subtitle_ocr_console.Utils;

namespace subtitle_ocr_console.OCR;

public class LabeledImageData
{
    private static int _targetHeight = 32;
    private static int _minWidth = 4;
    private static float _minRandomScale = 0.85f;
    private static float _maxRandomScale = 1.25f;
    private static double _binaryThreshold = 0.5;

    private static (char, char)[] _alphabet =
    {
        ('A', 'Z'),
        ('a', 'z'),
        ('0', '9'),
		//('!', '!'),
		//('\'', '\''),
		//(',', '.'),
		//('?', '?')
	};

    private static int _fontSize = 48;

    private static string[] _fontFamilies =
    {
        "Montserrat",
        "OpenSans",
        "Yrsa",
        "Exo"
    };

    private static FontStyle[] _fontStyles =
    {
        FontStyle.Regular,
        FontStyle.Bold,
        FontStyle.Italic,
        FontStyle.BoldItalic
    };

    public class Line
    {
        public int Image { get; }
        public string Text { get; }

        public Line(int image, string text)
        {
            Image = image;
            Text = text;
        }
    };

    public string ImageExtension { get; } = ".png";
    public List<char> Classes { get; } = new();
    public List<Line> Lines { get; } = new();

    private int _alphabetSize;

    public LabeledImageData()
    {
        // Calculate total number of characters in alphabet
        _alphabetSize = 0;
        foreach ((char first, char last) in _alphabet)
        {
            for (int i = first; i <= last; i++)
            {
                Classes.Add((char)i);
            }

            _alphabetSize += last - first + 1;
        }
    }

    private char GetCharacterFromIndex(int index)
    {
        int k = 0;
        char character = (char)0;

        foreach ((char first, char last) in _alphabet)
        {
            int size = last - first + 1;
            if (index >= k && index < k + size)
            {
                character = (char)(first + index - k);
                break;
            }

            k += size;
        }
        if (k == _alphabetSize)
        {
            // Shouldn't be possible, but just in case
            throw new Exception("Could not get character from random index");
        }

        return character;
    }

    private string[] GenerateStrings(int numStrings, int numCharacters)
    {
        var strings = new string[numStrings];
        var randomGenerator = new Random();

        for (var i = 0; i < numStrings; i++)
        {
            int numChars = randomGenerator.Next(1, numCharacters + 1);
            var builder = new StringBuilder(numChars);
            for (var j = 0; j < numChars; j++)
            {
                var charIndex = randomGenerator.Next(0, _alphabetSize);
                char character = GetCharacterFromIndex(charIndex);

                builder.Append(character);
            }

            strings[i] = builder.ToString();
        }

        return strings;
    }

    public void Generate(int numStrings, int numCharacters, string outputDir)
    {
        var strings = GenerateStrings(numStrings, numCharacters);

        var randomGenerator = new Random();
        int count = 0;
        FontCollection collection = new();
        foreach (string fontFamily in _fontFamilies)
        {
            foreach (FontStyle style in _fontStyles)
            {
                // Create font
                FontFamily family = collection.Add("fonts/" + fontFamily + "/static/" + fontFamily + "-" + style.ToString() + ".ttf");
                Font font = family.CreateFont(_fontSize, style);
                TextOptions options = new TextOptions(font);

                foreach (string text in strings)
                {
                    // Get text glyphs
                    IPathCollection glyphs = TextBuilder.GenerateGlyphs(text, options);

                    // Scale so height fills render area and apply some random stretching on x 
                    var scale = (float)_targetHeight / glyphs.Bounds.Height;
                    var rx = _minRandomScale + (_maxRandomScale - _minRandomScale) * randomGenerator.NextSingle();
                    glyphs = glyphs.Scale(scale * rx, scale);

                    // Translate so the text starts at leftmost and is centered vertically
                    glyphs = glyphs.Translate(-glyphs.Bounds.X, -glyphs.Bounds.Y);

                    var image = new Image<Rgba32>(Math.Max(_minWidth, (int)Math.Ceiling(glyphs.Bounds.Width)), _targetHeight);
                    image.Mutate(ctx =>
                        ctx.Fill(new DrawingOptions()
                        {
                            ShapeOptions = new ShapeOptions()
                            {
                                IntersectionRule = IntersectionRule.Nonzero
                            }
                        }, Color.White, glyphs)
                    );

                    var binarized = ImageBinarizer.Binarize(image, _binaryThreshold);
                    binarized.Save(outputDir + "/" + count + ImageExtension);

                    Lines.Add(new Line(count, text));

                    count++;
                }
            }
        }

        string jsonString = JsonSerializer.Serialize(this);
        File.WriteAllText(outputDir + "/labels.json", jsonString);
    }
}
