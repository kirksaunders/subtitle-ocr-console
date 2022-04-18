﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
        else if (args[0] == "infer")
        {
            Infer(args.Length > 1 ? args[1..^0] : new string[] { });
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
            throw new ArgumentException("Missing max number of strings to generate");
        }
        else if (args.Length < 2)
        {
            throw new ArgumentException("Missing max number of characters per image");
        }
        else if (args.Length < 3)
        {
            throw new ArgumentException("Missing random string generation rate");
        }
        else if (args.Length < 4)
        {
            throw new ArgumentException("Missing validation boolean");
        }
        else if (args.Length < 5)
        {
            throw new ArgumentException("Missing line data file path");
        }
        else if (args.Length < 6)
        {
            throw new ArgumentException("Missing directory to save data to");
        }

        int numStrings = Int32.Parse(args[0]);
        int numCharacters = Int32.Parse(args[1]);
        int randomStringRate = Int32.Parse(args[2]);
        bool validation = Boolean.Parse(args[3]);

        Codec codec = new(
            ('A', 'Z', CodecCharacterType.Letter),
            ('a', 'z', CodecCharacterType.Letter),
            ('0', '9', CodecCharacterType.Digit),
            (' ', ' ', CodecCharacterType.Whitespace),
            (',', ',', CodecCharacterType.Punctuation),
            ('.', '.', CodecCharacterType.Punctuation),
            ('!', '!', CodecCharacterType.Punctuation),
            ('?', '?', CodecCharacterType.Punctuation),
            ('-', '-', CodecCharacterType.Punctuation),
            ('"', '"', CodecCharacterType.Punctuation),
            ('\'', '\'', CodecCharacterType.Punctuation),
            ('(', ')', CodecCharacterType.Punctuation),
            (':', ';', CodecCharacterType.Punctuation)
        );
        LanguageModel model = new(codec, args[4]);

        codec.Save(args[5] + "/codec.json");

        var data = new LabeledImageData(codec);
        data.Generate(numStrings, numCharacters, randomStringRate, validation, args[4], args[5]);
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
            ('0', '9', CodecCharacterType.Digit),
            (' ', ' ', CodecCharacterType.Whitespace),
            (',', ',', CodecCharacterType.Punctuation),
            ('.', '.', CodecCharacterType.Punctuation),
            ('!', '!', CodecCharacterType.Punctuation),
            ('?', '?', CodecCharacterType.Punctuation),
            ('-', '-', CodecCharacterType.Punctuation),
            ('"', '"', CodecCharacterType.Punctuation),
            ('\'', '\'', CodecCharacterType.Punctuation),
            ('(', ')', CodecCharacterType.Punctuation),
            (':', ';', CodecCharacterType.Punctuation)
        );
        LanguageModel model = new(codec, args[0]);
    }

    static void Infer(string[] args)
    {
        if (args.Length < 1)
        {
            throw new ArgumentException("Missing path of saved ONNX model");
        }
        else if (args.Length < 2)
        {
            throw new ArgumentException("Missing path of one or more image files");
        }

        var model = new InferenceModel(args[0]);

        List<Image<A8>> images = new();
        for (var i = 1; i < args.Length; i++)
        {
            using (var image = Image.Load(args[i]))
            {
                var resized = image.CloneAs<A8>();
                resized.Mutate(ctx =>
                    ctx.Resize(0, 32)
                );

                images.Add(resized);
            }
        }

        var strings = model.Infer(images);
        foreach (var str in strings)
        {
            Console.WriteLine(str);
        }
    }
}
