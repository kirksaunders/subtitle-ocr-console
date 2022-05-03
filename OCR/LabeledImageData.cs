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
    private const int _targetHeight = 32;
    private const int _minWidth = 4;
    private const int _fontSize = 48;

    // NOTE: Some fonts have their italic forms disabled (by changing their file extension)
    //       because ImageSharp doesn't render them correctly.
    private static readonly string[] _trainingFontFamilies =
    {
        "Bitter",
        "Cabin",
        "EBGaramond",
        "Exo",
        "IBMPlexMono",
        "JetBrainsMono",
        "Jost",
        "Lora",
        "Lato",
        "Montserrat",
        "NanumGothic",
        "OpenSans",
        "Poppins",
        "PTSans",
        "Rubik",
        "Saira",
        "Yrsa"
    };

    private static readonly string[] _validationFontFamilies =
    {
        "Besley",
        "LibreFranklin",
        "Mukta",
        "NotoSansDisplay",
        "Piazzolla",
        "SourceSans3",
        "STIXTwoText",
        "TitilliumWeb",
        "UbuntuMono"
    };

    private static readonly FontStyle[] _fontStyles =
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

    private readonly Codec _codec;
    private DirectoryInfo? _directory;

    public class SerializableData
    {
        public string ImageExtension { get; set; } = new("");
        public List<Line> Lines { get; set; } = new();
    }

    public LabeledImageData(Codec codec)
    {
        _codec = codec;
    }

    public LabeledImageData(Codec codec, DirectoryInfo savePath)
    {
        _codec = codec;
        _directory = savePath;

        string jsonString = File.ReadAllText(savePath.FullName + "/labels.json");
        SerializableData? data = JsonSerializer.Deserialize<SerializableData>(jsonString);

        if (data == null)
        {
            throw new ArgumentException("Unable to read image data labels from file");
        }

        Lines = data.Lines;
        ImageExtension = data.ImageExtension;
    }

    public IEnumerable<IEnumerable<(Image<A8>, string)>> GetBatchedData(int batchSize)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentException("Batch size must be positive nonzero");
        }
        if (_directory == null)
        {
            throw new ArgumentNullException("Directory is null. Dataset is empty or uninitialized?");
        }

        for (var i = 0; i < Lines.Count; i += batchSize)
        {
            var end = Math.Min(i + batchSize, Lines.Count);
            List<(Image<A8>, string)> data = new(end - i);

            for (var j = i; j < end; j++)
            {
                Image<A8> image = Image.Load<A8>(_directory.FullName + "/" + Lines[j].Image + ImageExtension);
                data.Add((image, Lines[j].Text));
            }

            yield return data;
        }
    }

    public IEnumerable<(Image<A8>, string)> GetData()
    {
        if (_directory == null)
        {
            throw new ArgumentNullException("Directory is null. Dataset is empty or uninitialized?");
        }

        foreach (var line in Lines)
        {
            Image<A8> image = Image.Load<A8>(_directory.FullName + "/" + line.Image + ImageExtension);
            yield return (image, line.Text);
        }
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
                character = _codec.GetCharacter(charIndex)
                            ?? throw new ArgumentNullException("This code should be unreachable");
            } while ((j == 0 || j == numChars - 1) && character.Type == CodecCharacterType.Whitespace);

            builder.Append(character.Char);
        }

        return builder.ToString();
    }

    private IEnumerable<string> GenerateStrings(int maxNumStrings, int maxNumCharacters,
                                                int randomStringRate, IEnumerable<FileInfo> lineDataPaths)
    {
        var randomGenerator = new Random();

        int numStrings = 0;
        int numRealStrings = 0;
        StringBuilder builder = new(maxNumCharacters);
        int numChars = randomGenerator.Next(1, maxNumCharacters + 1);
        foreach (var lineDataPath in lineDataPaths)
        {
            var lines = LineDataReader.ReadLines(_codec, lineDataPath);
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

                        if (randomStringRate > 0 && numRealStrings % randomStringRate == 0 && numStrings < maxNumStrings)
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

                    if (randomStringRate > 0 && numRealStrings % randomStringRate == 0 && numStrings < maxNumStrings)
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
    }

    private static List<TextOptions> GenerateFonts(bool validation)
    {
        var families = validation ? _validationFontFamilies : _trainingFontFamilies;
        List<TextOptions> fonts = new();

        FontCollection collection = new();
        foreach (string fontFamily in families)
        {
            foreach (FontStyle style in _fontStyles)
            {
                // Some fonts don't have all font styles
                var fileInfo = new FileInfo("fonts/" + fontFamily + "/static/" + fontFamily + "-" + style.ToString() + ".ttf");
                if (fileInfo.Exists)
                {
                    // Create font
                    FontFamily family = collection.Add(fileInfo.FullName);
                    Font font = family.CreateFont(_fontSize, style);
                    TextOptions options = new(font);

                    fonts.Add(options);
                }
            }
        }

        return fonts;
    }

    public static void TestFonts(Codec codec, DirectoryInfo outputDir)
    {
        var families = _trainingFontFamilies.Concat(_validationFontFamilies);
        FontCollection collection = new();
        foreach (string fontFamily in families)
        {
            foreach (FontStyle style in _fontStyles)
            {
                // Some fonts don't have all font styles
                var fileInfo = new FileInfo("fonts/" + fontFamily + "/static/" + fontFamily + "-" + style.ToString() + ".ttf");
                if (fileInfo.Exists)
                {
                    // Create font
                    FontFamily family = collection.Add(fileInfo.FullName);
                    Font font = family.CreateFont(_fontSize, style);
                    TextOptions options = new(font);

                    // Render all of codec
                    var builder = new StringBuilder(codec.Count);
                    for (var i = 0; i < codec.Count; i++)
                    {
                        builder.Append(codec.GetCharacter(i)?.Char);
                    }

                    IPathCollection glyphs = TextBuilder.GenerateGlyphs(builder.ToString(), options);

                    // Scale so height fills render area and apply some random stretching on x 
                    var scale = (float)_targetHeight / glyphs.Bounds.Height;
                    glyphs = glyphs.Scale(scale);

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

                    image.Save(outputDir.FullName + "/" + fontFamily + "_" + style + ".png");
                }
            }
        }
    }

    public void Generate(int maxNumStrings, int maxNumCharacters, int randomStringRate,
                         bool validation, IEnumerable<FileInfo> lineDataPaths, DirectoryInfo outputDir)
    {
        _directory = outputDir;

        var strings = GenerateStrings(maxNumStrings, maxNumCharacters, randomStringRate, lineDataPaths);
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
            glyphs = glyphs.Scale(scale);

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

            image.Save(outputDir.FullName + "/" + count + ImageExtension);
            Lines.Add(new Line(count, text));

            count++;
        }

        var data = new SerializableData()
        {
            Lines = this.Lines,
            ImageExtension = this.ImageExtension,
        };
        string jsonString = JsonSerializer.Serialize(data);
        File.WriteAllText(outputDir.FullName + "/labels.json", jsonString);
    }
}
