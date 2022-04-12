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

    private Codec _codec;
    private LanguageModel _model;

    public LabeledImageData(Codec codec, LanguageModel languageModel)
    {
        _codec = codec;
        _model = languageModel;
    }

    private IEnumerable<string> GenerateStrings(int maxNumStrings, int maxNumCharacters, int randomStringRate, string lineDataPath)
    {
        var randomGenerator = new Random();
        var lines = LineDataReader.ReadLines(_codec, lineDataPath);

        int numStrings = 0;
        int numRealStrings = 0;
        foreach (string line in lines)
        {
            if (numStrings >= maxNumStrings)
            {
                break;
            }

            yield return line;
            numStrings++;
            numRealStrings++;

            if (numRealStrings % randomStringRate == 0 && numStrings < maxNumStrings)
            {
                int numChars = randomGenerator.Next(1, maxNumCharacters + 1);
                var builder = new StringBuilder(numChars);
                for (var j = 0; j < numChars; j++)
                {
                    builder.Append(_model.SampleCharacterUniform().Char);
                }

                // ImageSharp throws an exception if '/' is the first/last character of a string?
                // At least, this is true with the Montserrat font.
                if (builder[^1] == '/')
                {
                    builder.Append('a');
                }
                if (builder[0] == '/')
                {
                    builder.Insert(0, 'a');
                }

                yield return builder.ToString();
                numStrings++;
            }
        }
    }

    public void Generate(int maxNumStrings, int maxNumCharacters, int randomStringRate, string lineDataPath, string outputDir)
    {
        var strings = GenerateStrings(maxNumStrings, maxNumCharacters, randomStringRate, lineDataPath);

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

                    //var binarized = ImageBinarizer.Binarize(image, _binaryThreshold);
                    //binarized.Save(outputDir + "/" + count + ImageExtension);

                    image.CloneAs<A8>().Save(outputDir + "/" + count + ImageExtension);

                    Lines.Add(new Line(count, text));

                    count++;
                }
            }
        }

        string jsonString = JsonSerializer.Serialize(this);
        File.WriteAllText(outputDir + "/labels.json", jsonString);
    }
}
