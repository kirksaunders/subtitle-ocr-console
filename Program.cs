using subtitle_ocr_console.OCR;
using subtitle_ocr_console.Subtitles.PGS;
using subtitle_ocr_console.Utils;

static class ProgramEntry
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            throw new ArgumentException("Missing command line args: specify which entry point to use");
        }

        if (args[0] == "test-pgs")
        {
            ReadPGS(args.Length > 1 ? args[1..^0] : new string[] { });
        }
        else if (args[0] == "generate-data")
        {
            GenerateData(args.Length > 1 ? args[1..^0] : new string[] { });
        }
        else if (args[0] == "generate-language-model")
        {
            GenerateLanguageModel(args.Length > 1 ? args[1..^0] : new string[] { });
        }
        else
        {
            throw new ArgumentException($"Unknown command line argument: {args[0]}");
        }
    }

    static void ReadPGS(string[] args)
    {
        if (args.Length < 1)
        {
            throw new ArgumentException("Missing path of PGS file");
        }
        else if (args.Length < 2)
        {
            throw new ArgumentException("Missing directory to save output images to");
        }

        using (var stream = File.Open(args[0], FileMode.Open))
        {
            using (var reader = new EndiannessAwareBinaryReader(stream, System.Text.Encoding.UTF8, false, EndiannessAwareBinaryReader.Endianness.Big))
            {
                var pgs = new PGSReader(reader);
                pgs.WriteImages(args[1]);
            }
        }
    }

    static void GenerateData(string[] args)
    {
        if (args.Length < 1)
        {
            throw new ArgumentException("Missing number of images to generate");
        }
        else if (args.Length < 2)
        {
            throw new ArgumentException("Missing max number of characters per image");
        }
        else if (args.Length < 3)
        {
            throw new ArgumentException("Missing directory to save data to");
        }

        int numImages = Int32.Parse(args[0]);
        int numCharacters = Int32.Parse(args[1]);

        var data = new LabeledImageData();
        data.Generate(numImages, numCharacters, args[2]);
    }

    static void GenerateLanguageModel(string[] args)
    {
        if (args.Length < 1)
        {
            throw new ArgumentException("Missing path of dictionary file");
        }

        Codec codec = new(
            ('A', 'Z', CodecCharacterType.Letter),
            ('a', 'z', CodecCharacterType.Letter),
            ('0', '9', CodecCharacterType.Letter),
            ('-', '-', CodecCharacterType.Punctuation),
            (' ', ' ', CodecCharacterType.Whitespace),
            ('.', '.', CodecCharacterType.Punctuation),
            ('!', '!', CodecCharacterType.Punctuation),
            (',', ',', CodecCharacterType.Punctuation),
            ('\'', '\'', CodecCharacterType.Punctuation),
            ('/', '/', CodecCharacterType.Punctuation),
            ('&', '&', CodecCharacterType.Punctuation)
        );
        LanguageModel model = new(codec, args[0]);
    }
}
