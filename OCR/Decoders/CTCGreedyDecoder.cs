using Microsoft.ML.OnnxRuntime.Tensors;

namespace subtitle_ocr_console.OCR.Decoders;

public static class CTCGreedyDecoder
{
    public static List<Tensor<Int64>> Decode(Tensor<float> probabilities, Tensor<Int64> lengths, int blankIndex = 0)
    {
        var batchSize = probabilities.Dimensions[0];
        var numClasses = probabilities.Dimensions[2];

        var outTensors = new List<Tensor<Int64>>(batchSize);
        for (var i = 0; i < batchSize; i++)
        {
            var numPreds = (int)lengths[i, 0];

            // Calculate max character class for each prediction
            var maxes = new List<Int64>(numPreds);
            for (var j = 0; j < numPreds; j++)
            {
                int max = 0;
                for (var k = 1; k < numClasses; k++)
                {
                    if (probabilities[i, j, k] > probabilities[i, j, max])
                    {
                        max = k;
                    }
                }
                maxes.Add(max);
            }

            // Remove adjacent duplicates
            var duplicatesRemoved = new List<Int64>(numPreds);
            for (var j = 0; j < numPreds; j++)
            {
                if (j == 0 || duplicatesRemoved[^1] != maxes[j])
                {
                    duplicatesRemoved.Add(maxes[j]);
                }
            }

            // Remove blanks
            var blanksRemoved = new List<Int64>(duplicatesRemoved.Count);
            for (var j = 0; j < duplicatesRemoved.Count; j++)
            {
                if (duplicatesRemoved[j] != blankIndex)
                {
                    blanksRemoved.Add(duplicatesRemoved[j]);
                }
            }

            var outTensor = new DenseTensor<Int64>(new Memory<Int64>(blanksRemoved.ToArray()), new int[] { blanksRemoved.Count });
            outTensors.Add(outTensor);
        }

        return outTensors;
    }
}