using Microsoft.ML.OnnxRuntime.Tensors;

using subtitle_ocr_console.Utils;
using static subtitle_ocr_console.Utils.Logarithms;

namespace subtitle_ocr_console.OCR.Decoders;

public static class CTCBeamSearchDecoder
{
    public static List<Tensor<int>> Decode(Tensor<float> probabilities, Tensor<Int64> lengths, int beamWidth, int blankIndex = 0)
    {
        int batchSize = probabilities.Dimensions[0];
        int numClasses = probabilities.Dimensions[2];

        var outTensors = new List<Tensor<int>>(batchSize);

        FixedSizeHeap<BeamEntry> leaves = new(beamWidth);
        for (var i = 0; i < batchSize; i++)
        {
            BeamEntry root = new(null, blankIndex);
            root.NewP.Total = LOG_1;
            root.NewP.Blank = LOG_1;

            // Ensure leaves has been cleared since last run and add root
            leaves.Clear();
            leaves.Add(root);

            int numPreds = (int)lengths[i, 0];

            for (var t = 0; t < numPreds; t++)
            {
                var branches = leaves.ToList();
                leaves.Clear();

                // Move branch probabilities forward a time step
                foreach (var b in branches)
                {
                    b.OldP = b.NewP;
                }

                foreach (var b in branches)
                {
                    if (b.Parent != null) // if not the root
                    {
                        if (b.Parent.Active())
                        {
                            // If last two sequence characters are identical:
                            // Plabel(l=acc @ t=6) = (Plabel(l=acc @ t=5)
                            //                        + Pblank(l=ac @ t=5))
                            // else:
                            // Plabel(l=abc @ t=6) = (Plabel(l=abc @ t=5)
                            //                        + P(l=ab @ t=5))
                            var prev = b.Label == b.Parent.Label ? b.Parent.OldP.Blank : b.Parent.OldP.Total;
                            // TODO: Apply language model here (add to prev)
                            b.NewP.Label = LogAddExp(b.NewP.Label, prev);
                        }

                        // Plabel(l=abc @ t=6) *= P(c @ 6)
                        b.NewP.Label += probabilities[i, t, b.Label];
                    }

                    // Pblank(l=abc @ t=6) = P(l=abc @ t=5) * P(- @ 6)
                    b.NewP.Blank = b.OldP.Total + probabilities[i, t, blankIndex];
                    // P(l=abc @ t=6) = Plabel(l=abc @ t=6) + Pblank(l=abc @ t=6)
                    b.NewP.Total = LogAddExp(b.NewP.Blank, b.NewP.Label);

                    leaves.Add(b);
                }

                // A new leaf (represented by its BeamProbability) is a candidate
                // iff its total probability is nonzero and either the beam list
                // isn't full, or the lowest probability entry in the beam has a
                // lower probability than the leaf.
                var isCandidate = (BeamEntry.Probability prob) => prob.Total > LOG_0 &&
                                        (leaves.Count < beamWidth || prob.Total > leaves.GetMinimum().NewP.Total);

                // Grow new leaves
                foreach (var b in branches)
                {
                    if (!isCandidate(b.OldP))
                    {
                        continue;
                    }

                    for (var label = 0; label < numClasses; label++)
                    {
                        // Blank character case already handled above
                        if (label == blankIndex)
                        {
                            continue;
                        }

                        var logit = probabilities[i, t, label];

                        if (logit <= -9.0f)
                        {
                            continue;
                        }

                        var c = b.GetChild(label);
                        if (!c.Active())
                        {
                            // Pblank(l=abcd @ t=6) = 0
                            c.NewP.Blank = LOG_0;

                            // If new child label is identical to beam label:
                            //   Plabel(l=abcc @ t=6) = Pblank(l=abc @ t=5) * P(c @ 6)
                            // Otherwise:
                            //   Plabel(l=abcd @ t=6) = P(l=abc @ t=5) * P(d @ 6)
                            // TODO: Language model here?
                            var prev = b.Label == c.Label ? b.OldP.Blank : b.OldP.Total;
                            // TODO: And also LM here (add to prev)
                            c.NewP.Label = logit + prev;

                            // P(l=abcd @ t=6) = Plabel(l=abcd @ t=6)
                            c.NewP.Total = c.NewP.Label;

                            if (isCandidate(c.NewP))
                            {
                                // This will replace the current minimum only if c is better
                                // or leaves isn't full.
                                leaves.Add(c);
                            }
                            else
                            {
                                // Deactivate child
                                c.OldP.Reset();
                                c.NewP.Reset();
                            }
                        }
                    }
                }
            }

            // Get maximum branch
            var finalBranches = leaves.ToList();
            BeamEntry best = finalBranches[0];
            for (var j = 1; j < finalBranches.Count; j++)
            {
                if (finalBranches[j] > best)
                {
                    best = finalBranches[j];
                }
            }

            // Turn branch sequence into tensor
            var seq = best.LabelSequence();
            var outTensor = new DenseTensor<int>(new Memory<int>(seq.ToArray()), new int[] { seq.Count });
            outTensors.Add(outTensor);
        }

        return outTensors;
    }
}