using System.Text;
using System.Text.Json;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace subtitle_ocr_console.OCR;

public class LabeledImageData
{
    private static int _targetHeight = 32;
    private static int _minWidth = 4;
    private static float _minRandomScale = 0.85f;
    private static float _maxRandomScale = 1.35f;
    private static int _fontSize = 48;

    private static string[] _trainingFontFamilies =
    {
        "Montserrat",
        "OpenSans",
        "Yrsa",
        "Exo",
        "Lora",
        "Bitter",
        "EBGaramond",
        "Saira",
        "Jost"
    };

    private static string[] _validationFontFamilies =
    {
        "SourceSans3",
        "Besley",
        "STIXTwoText",
        "Piazzolla",
        "LibreFranklin",
        "NotoSansDisplay"
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
    public List<Line> Lines { get; } = new();

    private Codec _codec;

    public LabeledImageData(Codec codec)
    {
        _codec = codec;
    }

    public LabeledImageData(Codec codec, string path)
    {
        _codec = codec;

        string jsonString = File.ReadAllText(path + "/labels.json");
        List<Line>? lines = JsonSerializer.Deserialize<List<Line>>(jsonString);

        if (lines == null)
        {
            throw new ArgumentException("Unable to read image data labels from file");
        }

        Lines = lines;
    }

    private string GenerateRandomString(StringBuilder builder, int numChars, Random randomGenerator)
    {
        builder.Clear();
        for (var j = 0; j < numChars; j++)
        {
            CodecCharacter character;

            // Prevent first or last char from being whitespace
            do
            {
                int charIndex = randomGenerator.Next(_codec.Count);
                character = _codec.GetCharacter(charIndex) ?? throw new ArgumentNullException("This code should be unreachable");
            } while ((j == 0 || j == numChars - 1) && character.Type == CodecCharacterType.Whitespace);

            builder.Append(character.Char);
        }

        return builder.ToString();
    }

    private IEnumerable<string> GenerateStrings(int maxNumStrings, int maxNumCharacters, int randomStringRate, string lineDataPath)
    {
        var randomGenerator = new Random();
        var lines = LineDataReader.ReadLines(_codec, lineDataPath);

        int numStrings = 0;
        int numRealStrings = 0;
        StringBuilder builder = new(maxNumCharacters);
        int numChars = randomGenerator.Next(1, maxNumCharacters + 1);
        foreach (string line in lines)
        {
            if (numStrings >= maxNumStrings)
            {
                break;
            }

            foreach (string word in line.Split(' '))
            {
                if (numStrings >= maxNumStrings)
                {
                    break;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(word);

                if (builder.Length >= numChars)
                {
                    yield return builder.ToString();
                    numStrings++;
                    numRealStrings++;

                    if (numRealStrings % randomStringRate == 0 && numStrings < maxNumStrings)
                    {
                        numChars = randomGenerator.Next(1, maxNumCharacters + 1);
                        yield return GenerateRandomString(builder, numChars, randomGenerator);
                        numStrings++;
                    }

                    builder.Clear();
                    numChars = randomGenerator.Next(1, maxNumCharacters + 1);
                }
            }

            if (builder.Length > 0 && numStrings < maxNumStrings)
            {
                yield return builder.ToString();
                numStrings++;
                numRealStrings++;

                if (numRealStrings % randomStringRate == 0 && numStrings < maxNumStrings)
                {
                    numChars = randomGenerator.Next(1, maxNumCharacters + 1);
                    yield return GenerateRandomString(builder, numChars, randomGenerator);
                    numStrings++;
                }

                builder.Clear();
                numChars = randomGenerator.Next(1, maxNumCharacters + 1);
            }
        }
    }

    private List<TextOptions> GenerateFonts(bool validation)
    {
        var families = validation ? _validationFontFamilies : _trainingFontFamilies;
        List<TextOptions> fonts = new();

        FontCollection collection = new();
        foreach (string fontFamily in families)
        {
            foreach (FontStyle style in _fontStyles)
            {
                // Create font
                FontFamily family = collection.Add("fonts/" + fontFamily + "/static/" + fontFamily + "-" + style.ToString() + ".ttf");
                Font font = family.CreateFont(_fontSize, style);
                TextOptions options = new TextOptions(font);

                fonts.Add(options);
            }
        }

        return fonts;
    }

    public void Generate(int maxNumStrings, int maxNumCharacters, int randomStringRate, bool validation, string lineDataPath, string outputDir)
    {
        var strings = GenerateStrings(maxNumStrings, maxNumCharacters, randomStringRate, lineDataPath);
        var fonts = GenerateFonts(validation);

        var randomGenerator = new Random();
        int count = 0;

        foreach (string text in strings)
        {
            // Choose random font
            var font = fonts[randomGenerator.Next(fonts.Count)];

            // Get text glyphs
            IPathCollection glyphs = TextBuilder.GenerateGlyphs(text, font);

            // Scale so height fills render area and apply some random stretching on x 
            var scale = (float)_targetHeight / glyphs.Bounds.Height;
            var rx = 1.0f;
            if (!validation)
            {
                rx = _minRandomScale + (_maxRandomScale - _minRandomScale) * randomGenerator.NextSingle();
            }
            glyphs = glyphs.Scale(scale * rx, scale);

            // Translate so the text starts at leftmost and is centered vertically
            glyphs = glyphs.Translate(-glyphs.Bounds.X, -glyphs.Bounds.Y);

            var image = new Image<A8>(Math.Max(_minWidth, (int)Math.Ceiling(glyphs.Bounds.Width)), _targetHeight);
            image.Mutate(ctx =>
                ctx.Fill(new DrawingOptions()
                {
                    ShapeOptions = new ShapeOptions()
                    {
                        IntersectionRule = IntersectionRule.Nonzero
                    }
                }, Color.White, glyphs)
            );

            image.Save(outputDir + "/" + count + ImageExtension);
            Lines.Add(new Line(count, text));

            count++;
        }

        string jsonString = JsonSerializer.Serialize(this);
        File.WriteAllText(outputDir + "/labels.json", jsonString);
    }
}
