/* NOTICE: The main algorithm within this file was based on code that is part of the TensorFlow project.
           In particular, the DecodeInternal method below is heavily based on the original C++
           source. TensorFlow's license follows.


                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "[]"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.

   Copyright [yyyy] [name of copyright owner]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

## Some of TensorFlow's code is derived from Caffe, which is subject to the following copyright notice:

COPYRIGHT

All contributions by the University of California:

Copyright (c) 2014, The Regents of the University of California (Regents)
All rights reserved.

All other contributions:

Copyright (c) 2014, the respective contributors
All rights reserved.

Caffe uses a shared copyright model: each contributor holds copyright over
their contributions to Caffe. The project versioning records all such
contribution and copyright details. If a contributor wants to further mark
their specific copyright on a particular contribution, they should indicate
their copyright solely in the commit message of the change when it is
committed.

LICENSE

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
   DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
   ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

CONTRIBUTION AGREEMENT

By contributing to the BVLC/caffe repository through pull-request, comment,
or otherwise, the contributor releases their content to the
license and copyright terms herein.
*/

using Microsoft.ML.OnnxRuntime.Tensors;

using static subtitle_ocr_console.Utils.Logarithms;

namespace subtitle_ocr_console.OCR.Decoders;

public static class CTCBeamDecoder
{
    private static float GetLanguageModelProbability(BeamEntry a, BeamEntry b, LanguageModel languageModel, int blankIndex)
    {
        float p;
        int bIdx = b.Label < blankIndex ? b.Label : b.Label - 1;
        if (a.Label == blankIndex)
        {
            p = (float)languageModel.GetProbability(bIdx);
        }
        else
        {
            int aIdx = a.Label < blankIndex ? a.Label : a.Label - 1;
            p = (float)languageModel.GetProbability(aIdx, bIdx);
        }
        if (p < 1e-6)
        {
            p = 1e-6f;
        }

        return 0.25f * MathF.Log(p);
    }

    public static List<Tensor<int>> Decode(Tensor<float> probabilities, Tensor<Int64> lengths,
                                           int beamWidth, LanguageModel? languageModel = null, int blankIndex = 0)
    {
        int batchSize = probabilities.Dimensions[0];
        int maxNumPreds = probabilities.Dimensions[1];
        int numClasses = probabilities.Dimensions[2];

        var outTensors = new List<Tensor<int>>(batchSize);
        BeamList leaves = new(beamWidth);
        for (var i = 0; i < batchSize; i++)
        {
            BeamEntry root = new(null, blankIndex);
            root.NewP.Total = LOG_1;
            root.NewP.Blank = LOG_1;

            // Ensure leaves has been cleared since last run and add root
            leaves.Clear();
            leaves.Add(root);

            int numPreds = (int)lengths[i, 0];

            int batchIndex = i * (maxNumPreds * numClasses);

            for (var t = 0; t < numPreds; t++)
            {
                var branches = leaves.AsSpan();
                leaves.SwapAndClear();

                int predIndex = t * numClasses;

                // Move branch probabilities forward a time step
                foreach (var b in branches)
                {
                    b.OldP = b.NewP;
                }

                // Extend branches without adding to label sequence
                foreach (var b in branches)
                {
                    if (b.Label != blankIndex) // if not the root
                    {
                        if (b.Parent != null && b.Parent.Active())
                        {
                            // If last two sequence characters are identical:
                            // Plabel(l=acc @ t=6) = (Plabel(l=acc @ t=5)
                            //                        + Pblank(l=ac @ t=5))
                            // else:
                            // Plabel(l=abc @ t=6) = (Plabel(l=abc @ t=5)
                            //                        + P(l=ab @ t=5))
                            var prev = b.Label == b.Parent.Label ? b.Parent.OldP.Blank : b.Parent.OldP.Total;
                            if (languageModel != null)
                            {
                                prev += GetLanguageModelProbability(b.Parent, b, languageModel, blankIndex);
                            }
                            b.NewP.Label = LogAddExp(b.NewP.Label, prev);
                        }

                        // Plabel(l=abc @ t=6) *= P(c @ 6)
                        // probabilities[i, t, b.Label] (this way of indexing is faster)
                        b.NewP.Label += probabilities.GetValue(batchIndex + predIndex + b.Label);
                    }

                    // Pblank(l=abc @ t=6) = P(l=abc @ t=5) * P(- @ 6)
                    b.NewP.Blank = b.OldP.Total + probabilities[i, t, blankIndex];
                    // P(l=abc @ t=6) = Plabel(l=abc @ t=6) + Pblank(l=abc @ t=6)
                    b.NewP.Total = LogAddExp(b.NewP.Blank, b.NewP.Label);

                    leaves.Add(b);
                }

                // Grow new leaves (extending label sequence)
                foreach (var b in branches)
                {
                    // A new leaf (represented by its BeamProbability) is a candidate
                    // iff its total probability is nonzero and either the beam list
                    // isn't full, or the lowest probability entry in the beam has a
                    // lower probability than the leaf.
                    if (b.OldP.Total <= LOG_0 || (leaves.Count >= beamWidth && b.OldP.Total <= leaves.GetMinimum().NewP.Total))
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

                        // probabilities[i, t, label] (this way of indexing is faster)
                        var logit = probabilities.GetValue(batchIndex + predIndex + label);

                        if (logit <= LOG_0)
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
                            if (languageModel != null)
                            {
                                prev += GetLanguageModelProbability(b, c, languageModel, blankIndex);
                            }
                            c.NewP.Label = logit + prev;

                            // P(l=abcd @ t=6) = Plabel(l=abcd @ t=6)
                            c.NewP.Total = c.NewP.Label;

                            // Only insert leaf if c is better than current minimum or leaves is not full
                            if (c.NewP.Total > LOG_0 && (leaves.Count < beamWidth || c.NewP.Total > leaves.GetMinimum().NewP.Total))
                            {
                                BeamEntry? removed = leaves.Add(c);
                                if (removed != null)
                                {
                                    removed.Delete();
                                }
                            }
                            else
                            {
                                // Deactivate child
                                c.Delete();
                            }
                        }
                    }
                }
            }

            // Get maximum branch
            var finalBranches = leaves.AsSpan();
            BeamEntry best = finalBranches[0];
            for (var j = 1; j < finalBranches.Length; j++)
            {
                if (finalBranches[j] > best)
                {
                    best = finalBranches[j];
                }
            }

            // Get best branch sequence
            var seq = best.LabelSequence();

            // Delete all branches
            foreach (var branch in finalBranches)
            {
                branch.Delete();
            }

            // Turn branch sequence into tensor
            var outTensor = new DenseTensor<int>(new Memory<int>(seq.ToArray()), new int[] { seq.Count });
            outTensors.Add(outTensor);
        }

        return outTensors;
    }
}