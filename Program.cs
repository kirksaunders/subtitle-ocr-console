﻿using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using subtitle_ocr_console.OCR;
using subtitle_ocr_console.Subtitles.PGS;
using subtitle_ocr_console.Utils;

static class ProgramEntry
{
    static void Main(string[] args)
    {
        // Create command line argument parsers
        var pgsCommand = new Command("parse-pgs", "Parses a PGS subtitle file and writes its images");
        pgsCommand.Add(new Argument<FileInfo>("path", "Path to PGS file").ExistingOnly());
        pgsCommand.Add(new Argument<DirectoryInfo>("out-dir", "Directory path to save images to").ExistingOnly());
        pgsCommand.Handler = CommandHandler.Create(ParsePGS);

        var codecCommand = new Command("generate-codec", "Generates a codec");
        codecCommand.Add(new Argument<FileInfo>("out-path", "Path to save the codec to").LegalFilePathsOnly());
        codecCommand.Add(new Argument<string>("letter-characters", "A single string containing all letters in codec"));
        codecCommand.Add(new Argument<string>("digit-characters", "A single string containing all digits in codec"));
        codecCommand.Add(new Argument<string>("punctuation-characters", "A single string containing all punctuation characters in codec"));
        codecCommand.Add(new Argument<string>("whitespace-characters", "A single string containing all whitespace characters in codec"));
        codecCommand.Handler = CommandHandler.Create(GenerateCodec);

        var dataCommand = new Command("generate-data", "Generates training/validation data for the CRNN model");
        dataCommand.Add(new Argument<int>("size", "Size of the data set (number of images)"));
        dataCommand.Add(new Argument<DirectoryInfo>("out-dir", "Directory path to save data to").ExistingOnly());
        dataCommand.Add(new Argument<FileInfo>("codec-path", "Path to the codec").ExistingOnly());
        dataCommand.Add(new Argument<FileInfo[]>("line-data", "Paths to files containing line data text").ExistingOnly());
        dataCommand.Add(new Option<int>(
            new string[] { "--max-chars", "-c" },
            () => 75,
            "Max number of characters (roughly) per image"
        ));
        dataCommand.Add(new Option<int>(
            new string[] { "--rand-rate", "-r" },
            () => 4,
            "Rate of random images. One random image for each <rand-rate> images from text data, or -1 for no random images"
        ));
        dataCommand.Add(new Option<bool>(
            new string[] { "--validation", "-v" },
            "Whether to generate validation or training data. Validation data uses different fonts and no data augmentation"
        ));
        dataCommand.Handler = CommandHandler.Create(GenerateData);

        var lmCommand = new Command("generate-lm", "Generates a language model");
        lmCommand.Add(new Argument<FileInfo>("out-path", "Path to save the language model to").LegalFilePathsOnly());
        lmCommand.Add(new Argument<FileInfo>("codec-path", "Path to the codec").ExistingOnly());
        lmCommand.Add(new Argument<FileInfo[]>("line-data", "Paths to files containing line data text").ExistingOnly());
        lmCommand.Handler = CommandHandler.Create(GenerateLanguageModel);

        var inferCommand = new Command("test-inference", "Runs a trained model on the given images");
        inferCommand.Add(new Argument<FileInfo>("model-path", "Path to the trained model").ExistingOnly());
        inferCommand.Add(new Argument<FileInfo>("codec-path", "Path to the codec").ExistingOnly());
        inferCommand.Add(new Argument<FileInfo[]>("image-paths", "Paths to images to recognize").ExistingOnly());
        inferCommand.Add(new Option<FileInfo>(
            new string[] { "--language-model", "-lm" },
            "Path to the language model").ExistingOnly()
        );
        inferCommand.Handler = CommandHandler.Create(TestInference);

        var rootCommand = new RootCommand("Command line tool for converting PGS subtitles to SRT subtitles using OCR")
        {
            pgsCommand,
            codecCommand,
            dataCommand,
            lmCommand,
            inferCommand
        };

        rootCommand.Invoke(args);
    }

    static void ParsePGS(FileInfo path, DirectoryInfo outDir)
    {
        using (var stream = path.Open(FileMode.Open))
        {
            using (var reader = new EndiannessAwareBinaryReader(stream, System.Text.Encoding.UTF8,
                                                                false, EndiannessAwareBinaryReader.Endianness.Big))
            {
                var pgs = new PGSReader(reader);
                pgs.WriteImages(outDir);
            }
        }

        Console.WriteLine($"Done. Files written to {outDir}");
    }

    static void GenerateData(int size, DirectoryInfo outDir, FileInfo codecPath,
                             IEnumerable<FileInfo> lineData, int maxChars, int randRate, bool validation)
    {
        if (size <= 0)
        {
            throw new ArgumentException("Dataset size should be positive nonzero");
        }
        if (maxChars <= 0)
        {
            throw new ArgumentException("Image max characters should be positive nonzero");
        }
        if (randRate == 0)
        {
            throw new ArgumentException("Random rate should be nonzero");
        }
        if (!lineData.Any())
        {
            throw new ArgumentException("At least one line data path must be supplied");
        }

        var codec = new Codec(codecPath);

        codec.Save(new FileInfo(outDir.FullName + "/codec.json"));

        var data = new LabeledImageData(codec);
        data.Generate(size, maxChars, randRate, validation, lineData, outDir);

        Console.WriteLine($"Done. Data written to {outDir}");
    }

    static void GenerateCodec(FileInfo outPath, string letterCharacters, string digitCharacters,
                              string punctuationCharacters, string whitespaceCharacters)
    {
        /* Codec codec = new(
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
        ); *

        /*
            Command to generate the above codec:
            dotnet run -- generate-codec codecs/en.json $'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz' $'0123456789' $',.!?-"\'():;' $' '
        */

        var charList = new List<(char, char, CodecCharacterType)>();
        foreach (char c in letterCharacters.ToCharArray())
        {
            charList.Add((c, c, CodecCharacterType.Letter));
        }
        foreach (char c in digitCharacters.ToCharArray())
        {
            charList.Add((c, c, CodecCharacterType.Digit));
        }
        foreach (char c in punctuationCharacters.ToCharArray())
        {
            charList.Add((c, c, CodecCharacterType.Punctuation));
        }
        foreach (char c in whitespaceCharacters.ToCharArray())
        {
            charList.Add((c, c, CodecCharacterType.Whitespace));
        }

        var codec = new Codec(charList.ToArray());
        codec.Save(outPath);

        Console.WriteLine($"Done. Codec written to {outPath}");
    }

    static void GenerateLanguageModel(FileInfo outPath, FileInfo codecPath, IEnumerable<FileInfo> lineData)
    {
        if (!lineData.Any())
        {
            throw new ArgumentException("At least one line data path must be supplied");
        }

        var codec = new Codec(codecPath);
        var model = new LanguageModel(codec, lineData);
        model.Save(outPath);

        Console.WriteLine($"Done. Model written to {outPath}");
    }

    static void TestInference(FileInfo modelPath, FileInfo codecPath, IEnumerable<FileInfo> imagePaths, FileInfo languageModel)
    {
        if (!imagePaths.Any())
        {
            throw new ArgumentException("At least one image path must be supplied");
        }

        var codec = new Codec(codecPath);
        var model = new InferenceModel(codec, modelPath);
        var langModel = languageModel != null ? new LanguageModel(codec, languageModel) : null;

        List<Image<A8>> images = new();
        foreach (var path in imagePaths)
        {
            using (var image = Image.Load(path.FullName))
            {
                var resized = image.CloneAs<A8>();
                resized.Mutate(ctx =>
                    ctx.Resize(0, 32)
                );

                images.Add(resized);
            }
        }


        Console.WriteLine("Recognized text:");

        var strings = model.Infer(images, langModel);
        foreach ((var path, var str) in imagePaths.Zip(strings))
        {
            Console.WriteLine($"{path} => {str}");
        }
    }
}
