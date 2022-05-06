using System.Text;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using subtitle_ocr_console.OCR.Decoders;
using static subtitle_ocr_console.Utils.Logarithms;

namespace subtitle_ocr_console.OCR;

public class InferenceModel
{
    public Codec _codec;
    private readonly InferenceSession _session;

    // TODO: Make this its own class so it can be saved/loaded to/from file
    private readonly List<(int, int, float)> _simMatrix = new();

    public InferenceModel(Codec codec, FileInfo savePath)
    {
        _codec = codec;
        _session = new(savePath.FullName/* , SessionOptions.MakeSessionOptionWithCudaProvider() */);

        _simMatrix.Add((_codec.GetCharacterIndex('I'), _codec.GetCharacterIndex('l'), 0.75f));
    }

    public InferenceModel(Codec codec, Stream inputStream)
    {
        _codec = codec;

        // Read model into memory and instantiate inference session via byte array
        byte[] bytes;
        using (var memoryStream = new MemoryStream())
        {
            inputStream.CopyTo(memoryStream);
            bytes = memoryStream.ToArray();
        }
        _session = new(bytes/* , SessionOptions.MakeSessionOptionWithCudaProvider() */);

        _simMatrix.Add((_codec.GetCharacterIndex('I'), _codec.GetCharacterIndex('l'), 0.75f));
    }

    public List<NamedOnnxValue> PrepareInput(List<Image<A8>> images)
    {
        // Reverse sort images by width
        //var sortedImages = images.OrderByDescending(a => a.Width).ToList();

        int batchSize = images.Count;
        int maxWidth = -1;
        foreach (var img in images)
        {
            if (img.Width > maxWidth)
            {
                maxWidth = img.Width;
            }
        }

        var imageTensor = new DenseTensor<float>(new int[] { batchSize, 1, maxWidth, 32 });
        var sizeTensor = new DenseTensor<int>(new int[] { batchSize, 3 });

        for (var i = 0; i < images.Count; i++)
        {
            images[i].ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < images[i].Height; y++)
                {
                    Span<A8> pixelRow = accessor.GetRowSpan(y);

                    for (var x = 0; x < images[i].Width; x++)
                    {
                        imageTensor[i, 0, x, y] = (float)pixelRow[x].PackedValue / 255;
                        sizeTensor[i, 0] = 1;
                        sizeTensor[i, 1] = images[i].Width;
                        sizeTensor[i, 2] = images[i].Height;
                    }
                }
            });
        }

        var input = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor<float>("images", imageTensor),
            NamedOnnxValue.CreateFromTensor<int>("sizes", sizeTensor)
        };

        return input;
    }

    public (Tensor<float>, Tensor<int>) FeedForward(List<NamedOnnxValue> input)
    {
        var outputs = _session.Run(input).ToList();
        var probs = outputs[0].AsTensor<float>();
        var sizes = outputs[1].AsTensor<int>();

        return (probs, sizes);
    }

    public List<string> Decode(Tensor<float> probs, Tensor<int> sizes, LanguageModel? languageModel)
    {
        int batchSize = sizes.Dimensions[0];

        // Apply similarity matrix
        var newProbs = probs.Clone();
        foreach ((var i, var j, var weight) in _simMatrix)
        {
            // TODO: Make this based on blankIndex (not hardcoded)
            int x = i + 1;
            int y = j + 1;
            float w = (float)weight * 0.5f;
            for (var b = 0; b < batchSize; b++)
            {
                int numPreds = (int)sizes[b, 0];
                for (var t = 0; t < numPreds; t++)
                {
                    float p1 = MathF.Exp(probs[b, t, x]);
                    float p2 = MathF.Exp(probs[b, t, y]);
                    if (p2 > p1)
                    {
                        float dif = MathF.Log((p2 - p1) * w);

                        newProbs[b, t, x] = LogAddExp(newProbs[b, t, x], dif);
                        newProbs[b, t, y] = LogSubExp(newProbs[b, t, y], dif);
                    }
                    else if (p1 > p2)
                    {
                        float dif = MathF.Log((p1 - p2) * w);

                        newProbs[b, t, x] = LogSubExp(newProbs[b, t, x], dif);
                        newProbs[b, t, y] = LogAddExp(newProbs[b, t, y], dif);
                    }
                }
            }
        }

        var sizesNew = new List<int>();
        for (var i = 0; i < batchSize; i++)
        {
            sizesNew.Add(sizes[i, 0]);
        }
        var numClasses = newProbs.Dimensions[2];
        CTCBeamDecoderExternal.DecoderOutput decoded;
        if (languageModel != null)
        {
            decoded = CTCBeamDecoderExternal.DecodeWithLm(
                newProbs,
                sizesNew,
                numClasses,
                50,
                0,
                languageModel.FirstCharProbs,
                languageModel.SecondCharProbsFlat,
                0.25f,
                1e-6f);
        }
        else
        {
            decoded = CTCBeamDecoderExternal.Decode(newProbs, sizesNew, numClasses, 50, 0);
        }

        // Get max sequence length
        int maxLen = 0;
        foreach (int len in decoded.lengths)
        {
            if (len > maxLen)
            {
                maxLen = len;
            }
        }

        var strings = new List<string>(batchSize);
        for (var batch = 0; batch < batchSize; batch++)
        {
            var len = (int)decoded.lengths[batch];
            var builder = new StringBuilder((int)len);
            for (var i = 0; i < (int)len; i++)
            {
                CodecCharacter character = _codec.GetCharacter((int)(decoded.sequences[batch * maxLen + i] - 1))
                                           ?? throw new ArgumentNullException("Index out of range for codec");
                builder.Append(character.Char);
            }
            strings.Add(builder.ToString());
        }

        return strings;
    }

    public List<string> Infer(List<Image<A8>> images, LanguageModel? languageModel = null)
    {
        var input = PrepareInput(images);
        (var probs, var sizes) = FeedForward(input);

        return Decode(probs, sizes, languageModel);
    }
}