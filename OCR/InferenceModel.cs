using System.Text;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using subtitle_ocr_console.OCR.Decoders;

namespace subtitle_ocr_console.OCR;

public class InferenceModel
{
    public Codec _codec;
    private InferenceSession _session;

    public InferenceModel(Codec codec, FileInfo savePath)
    {
        _codec = codec;
        _session = new(savePath.FullName);
    }

    public List<string> Infer(List<Image<A8>> images, LanguageModel? languageModel = null)
    {
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
        var sizeTensor = new DenseTensor<Int64>(new int[] { batchSize, 3 });

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
            NamedOnnxValue.CreateFromTensor<Int64>("sizes", sizeTensor)
        };

        var outputs = _session.Run(input).ToList();
        var probs = outputs[0].AsTensor<float>();
        var sizes = outputs[1].AsTensor<Int64>();

        var decoded = CTCBeamSearchDecoder.Decode(probs, sizes, 50, languageModel);

        var strings = new List<string>(batchSize);
        foreach (var seq in decoded)
        {
            var builder = new StringBuilder(seq.Dimensions[0]);
            for (var i = 0; i < seq.Dimensions[0]; i++)
            {
                CodecCharacter character = _codec.GetCharacter((int)seq[i] - 1) ?? throw new ArgumentNullException("Index out of range for codec");
                builder.Append(character.Char);
            }
            strings.Add(builder.ToString());
        }

        return strings;
    }
}