using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using subtitle_ocr_console.OCR;
using subtitle_ocr_console.Subtitles.PGS;
using subtitle_ocr_console.Subtitles.SRT;
using subtitle_ocr_console.Subtitles.Segmentation;
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

        var evalModelCommand = new Command("eval-model", "Evaluates the performance of a model on the given dataset");
        evalModelCommand.Add(new Argument<FileInfo>("model-path", "Path to the trained model").ExistingOnly());
        evalModelCommand.Add(new Argument<FileInfo>("codec-path", "Path to the codec").ExistingOnly());
        evalModelCommand.Add(new Argument<DirectoryInfo>("data-dir", "Path of directory containing the dataset").ExistingOnly());
        evalModelCommand.Add(new Option<FileInfo>(
            new string[] { "--language-model", "-lm" },
            "Path to the language model").ExistingOnly()
        );
        evalModelCommand.Add(new Option<int>(
            new string[] { "--skip", "-s" },
            () => -1,
            "Used to skip random images. For example, if rand-rate was 5 when generating the data, supply 5 here")
        );
        evalModelCommand.Handler = CommandHandler.Create(EvalModel);

        var evalTessCommand = new Command("eval-tesseract", "Evaluates the performance of Tesseract on the given dataset");
        evalTessCommand.Add(new Argument<DirectoryInfo>("tessdata-path", "Path to the tessdata directory").ExistingOnly());
        evalTessCommand.Add(new Argument<string>("language", "Tesseract language string"));
        evalTessCommand.Add(new Argument<FileInfo>("codec-path", "Path to the codec").ExistingOnly());
        evalTessCommand.Add(new Argument<DirectoryInfo>("data-dir", "Path of directory containing the dataset").ExistingOnly());
        evalTessCommand.Add(new Option<int>(
            new string[] { "--skip", "-s" },
            () => -1,
            "Used to skip random images. For example, if rand-rate was 5 when generating the data, supply 5 here")
        );
        evalTessCommand.Handler = CommandHandler.Create(EvalTesseract);

        var convertCommand = new Command("convert", "Converts a PGS subtitle file to SRT");
        convertCommand.Add(new Argument<FileInfo>("pgs-path", "Path to PGS file").ExistingOnly());
        convertCommand.Add(new Argument<FileInfo>("srt-path", "Path to save converted SRT file to").LegalFilePathsOnly());
        convertCommand.Add(new Argument<DirectoryInfo>("model-dir", "Directory containing model data (including codec and language model)").ExistingOnly());
        convertCommand.Handler = CommandHandler.Create(ConvertPGS);

        var rootCommand = new RootCommand("Command line tool for converting PGS subtitles to SRT subtitles using OCR")
        {
            pgsCommand,
            codecCommand,
            dataCommand,
            lmCommand,
            inferCommand,
            evalModelCommand,
            evalTessCommand,
            convertCommand
        };

        rootCommand.Invoke(args);
    }

    static void ParsePGS(FileInfo path, DirectoryInfo outDir)
    {
        PGSReader pgsReader;
        using (var stream = path.Open(FileMode.Open))
        {
            using (var reader = new EndiannessAwareBinaryReader(stream, System.Text.Encoding.UTF8,
                                                                false, EndiannessAwareBinaryReader.Endianness.Big))
            {
                pgsReader = new PGSReader(reader);
            }
        }

        int frameCount = 0;
        foreach (var frame in pgsReader.GetFrames())
        {
            int windowCount = 0;
            foreach (var img in frame.Images)
            {
                img.Img.Save($"{outDir.FullName}/frame_{frameCount}_window_{windowCount}.png");
                windowCount++;
            }
            frameCount++;
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

        Console.WriteLine("Recognized text:");

        var imagePathsArr = imagePaths.ToArray();

        // Only do 64 at a time
        for (int i = 0; i < imagePathsArr.Length; i += 64)
        {
            var end = Math.Min(i + 64, imagePathsArr.Length);
            var imgPaths = imagePathsArr[i..end];

            List<Image<A8>> images = new();
            foreach (var path in imgPaths)
            {
                using (Image<A8> image = Image.Load<A8>(path.FullName))
                {
                    var resized = image.Clone(ctx =>
                        ctx.Resize(0, 32)
                    );

                    images.Add(resized);
                }
            }

            var strings = model.Infer(images, langModel);
            foreach ((var path, var str) in imgPaths.Zip(strings))
            {
                Console.WriteLine($"{path} => {str}");
            }
        }
    }

    static void EvalModel(FileInfo modelPath, FileInfo codecPath, DirectoryInfo dataDir, FileInfo languageModel, int skip)
    {
        var codec = new Codec(codecPath);
        var model = new InferenceModel(codec, modelPath);
        var data = new LabeledImageData(codec, dataDir);
        var langModel = languageModel != null ? new LanguageModel(codec, languageModel) : null;

        // Do 64 at a time
        var totalErrors = 0;
        var totalCharacters = 0;
        var count = -1;
        foreach (var batch in data.GetBatchedData(64))
        {
            List<Image<A8>> images = new();
            List<string> texts = new();
            foreach ((var image, var text) in batch)
            {
                count++;
                if (skip > 0 && count % (skip + 1) == skip)
                {
                    continue;
                }
                images.Add(image);
                texts.Add(text);
            }

            var strings = model.Infer(images, langModel);
            foreach ((var text, var str) in texts.Zip(strings))
            {
                var dist = LevenshteinDistance.Distance(text, str);
                totalErrors += dist;
                totalCharacters += text.Length;
                Console.WriteLine($"Levenshtein distance: {dist}, {text} => {str}");
            }
        }

        double characterErrorRate = (double)totalErrors / totalCharacters;
        Console.WriteLine($"Character error rate: {characterErrorRate}");
    }

    static void EvalTesseract(DirectoryInfo tessdataPath, string language, FileInfo codecPath, DirectoryInfo dataDir, int skip)
    {
        var model = new TesseractModel(tessdataPath, language);
        var codec = new Codec(codecPath);
        var data = new LabeledImageData(codec, dataDir);

        var totalErrors = 0;
        var totalCharacters = 0;
        int count = -1;
        foreach ((var image, var text) in data.GetData())
        {
            count++;
            if (skip > 0 && count % (skip + 1) == skip)
            {
                continue;
            }

            // Zoom image out a bit (to give Tesseract a fair shot since it doesn't
            // like tightly cropped images)
            var newImage = new Image<Rgba32>(image.Width + 10, image.Height + 10);
            newImage.Mutate(ctx =>
                ctx.DrawImage(image, new Point(5, 5), 1.0f)
            );

            var str = model.Infer(newImage);
            var dist = LevenshteinDistance.Distance(text, str);
            totalErrors += dist;
            totalCharacters += text.Length;
            Console.WriteLine($"Levenshtein distance: {dist}, {text} => {str}");
        }

        double characterErrorRate = (double)totalErrors / totalCharacters;
        Console.WriteLine($"Character error rate: {characterErrorRate}");
    }

    static void ConvertPGS(FileInfo pgsPath, FileInfo srtPath, DirectoryInfo modelDir)
    {
        // Load model
        var codec = new Codec(new FileInfo(modelDir.FullName + "/codec.json"));
        var model = new InferenceModel(codec, new FileInfo(modelDir.FullName + "/model.onnx"));
        LanguageModel? langModel = null;
        var lmPath = new FileInfo(modelDir.FullName + "/lm.json");
        if (lmPath.Exists)
        {
            langModel = new LanguageModel(codec, lmPath);
        }

        PGSReader pgsReader;
        using (var stream = pgsPath.Open(FileMode.Open))
        {
            using (var reader = new EndiannessAwareBinaryReader(stream, System.Text.Encoding.UTF8,
                                                                false, EndiannessAwareBinaryReader.Endianness.Big))
            {
                pgsReader = new PGSReader(reader);
            }
        }

        SRT srt = new();
        SRTFrame? lastFrame = null;
        foreach (var frame in pgsReader.GetFrames())
        {
            var text = new StringBuilder();
            foreach (var img in frame.Images)
            {
                var binarized = ImageBinarizer.Binarize(img.Img, 0.5);
                var lines = LineSegmenter.Segment(binarized);

                // Ensure images are the proper height
                foreach (var line in lines)
                {
                    line.Mutate(ctx => ctx.Resize(0, 32));
                }

                var strings = model.Infer(lines, langModel);
                foreach (var str in strings)
                {
                    if (text.Length > 0)
                    {
                        text.Append('\n');
                    }
                    text.Append(str);
                }
            }

            if (lastFrame != null && lastFrame.Text.Length > 0)
            {
                lastFrame.EndTimestamp = frame.Timestamp;
                srt.AddFrame(lastFrame);
            }

            lastFrame = new SRTFrame(frame.Timestamp, text.ToString());
        }

        srt.Write(srtPath);

        Console.WriteLine($"Done. SRT file written to {srtPath}");
    }
}
