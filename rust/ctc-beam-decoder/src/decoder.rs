/* NOTICE: The main algorithm within this file was based on code that is part of the TensorFlow project.
           In particular, the decode_internal method below is heavily based on the original C++
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

// To better match .NET's naming scheme for structs, allow non_snake_case
#![allow(non_snake_case)]

use rnet::{net, Net};

use crate::beam_list::{BeamEntry, BeamList};
use crate::log_space::LogSpace;

rnet::root!();

#[derive(Net)]
pub struct DecoderOutput {
    pub Sequences: Vec<i32>,
    pub Lengths: Vec<i32>,
}

pub struct LanguageModel<'a> {
    pub first_char_probs: &'a [f32],
    pub second_char_probs: &'a [f32],
    pub weight: f32,
    pub min_prob: f32,
}

impl<'a> LanguageModel<'a> {
    fn get_probability(&self, a: &BeamEntry, b: &BeamEntry, blank_idx: i32) -> LogSpace {
        let num_classes = self.first_char_probs.len();
        let b_idx = if b.label < blank_idx {
            b.label
        } else {
            b.label - 1
        } as usize;
        let mut p = if a.label == blank_idx {
            self.first_char_probs[b_idx]
        } else {
            let a_idx = if a.label < blank_idx {
                a.label
            } else {
                a.label - 1
            } as usize;
            self.second_char_probs[a_idx * num_classes + b_idx]
        };
        if p < self.min_prob {
            p = self.min_prob;
        }

        LogSpace(self.weight * f32::ln(p))
    }
}

#[net]
pub fn decode(
    predictions: &[f32],
    lengths: &[i32],
    num_classes: i32,
    beam_width: i32,
    blank_idx: i32,
) -> DecoderOutput {
    decode_internal(
        predictions,
        lengths,
        num_classes,
        beam_width,
        blank_idx,
        None,
    )
}

#[net]
pub fn decode_with_lm(
    predictions: &[f32],
    lengths: &[i32],
    num_classes: i32,
    beam_width: i32,
    blank_idx: i32,
    first_char_probs: &[f32],
    second_char_probs: &[f32],
    lm_weight: f32,
    lm_min_prob: f32,
) -> DecoderOutput {
    decode_internal(
        predictions,
        lengths,
        num_classes,
        beam_width,
        blank_idx,
        Some(LanguageModel {
            first_char_probs,
            second_char_probs,
            weight: lm_weight,
            min_prob: lm_min_prob,
        }),
    )
}

pub fn decode_internal(
    predictions: &[f32],
    lengths: &[i32],
    num_classes: i32,
    beam_width: i32,
    blank_idx: i32,
    language_model: Option<LanguageModel>,
) -> DecoderOutput {
    let beam_width = beam_width as usize;
    let batch_size = lengths.len();

    // Get max prediction length
    let mut max_num_preds = 0;
    for i in 0..batch_size {
        if lengths[i] > max_num_preds {
            max_num_preds = lengths[i];
        }
    }

    let mut outputs = Vec::new();
    let mut leaves = BeamList::new(beam_width);
    for i in 0..batch_size {
        let batch_index = i * (max_num_preds as usize) * (num_classes as usize);

        // Ensure leaves has been cleared since last run and add root
        leaves.clear();
        leaves.add_root(blank_idx);

        let num_preds = lengths[i] as usize;
        for t in 0..num_preds {
            let time_index = t * (num_classes as usize);

            // Copy previous leaves to here and clear
            let branches = leaves.clone_heap();
            leaves.clear();

            // Move branch probabilities forward a time step
            for b in branches.iter() {
                leaves[*b].old_p = leaves[*b].new_p;
            }

            // Extend branches without adding to label sequence
            for b in branches.iter() {
                if leaves[*b].parent != usize::MAX {
                    // if not the root
                    let parent = &leaves[leaves[*b].parent];
                    let label = leaves[*b].label;
                    if parent.is_active() {
                        // If last two sequence characters are identical:
                        // Plabel(l=acc @ t=6) = (Plabel(l=acc @ t=5)
                        //                        + Pblank(l=ac @ t=5))
                        // else:
                        // Plabel(l=abc @ t=6) = (Plabel(l=abc @ t=5)
                        //                        + P(l=ab @ t=5))

                        let mut prev = if label == parent.label {
                            parent.old_p.blank
                        } else {
                            parent.old_p.total
                        };
                        if let Some(lm) = &language_model {
                            prev *= lm.get_probability(parent, &leaves[*b], blank_idx);
                        }
                        leaves[*b].new_p.label += prev;
                    }

                    // Plabel(l=abc @ t=6) *= P(c @ 6)
                    leaves[*b].new_p.label *=
                        predictions[batch_index + time_index + (label as usize)];
                }

                // Pblank(l=abc @ t=6) = P(l=abc @ t=5) * P(- @ 6)
                leaves[*b].new_p.blank = leaves[*b].old_p.total
                    * predictions[batch_index + time_index + (blank_idx as usize)];
                // P(l=abc @ t=6) = Plabel(l=abc @ t=6) + Pblank(l=abc @ t=6)
                leaves[*b].new_p.total = leaves[*b].new_p.blank + leaves[*b].new_p.label;

                if leaves[*b].new_p.total > LogSpace(f32::NEG_INFINITY) {
                    leaves.push(*b);
                }
            }

            // Grow new leaves (extending label sequence)
            for b in branches.iter() {
                // A new leaf (represented by its BeamProbability) is a candidate
                // iff its total probability is nonzero and either the beam list
                // isn't full, or the lowest probability entry in the beam has a
                // lower probability than the leaf.
                if leaves[*b].old_p.total <= LogSpace(f32::NEG_INFINITY)
                    || (leaves.len() >= beam_width
                        && leaves[*b].old_p.total <= leaves[leaves.min()].new_p.total)
                {
                    continue;
                }

                for label in 0..num_classes {
                    // Blank character case already handled above
                    if label == blank_idx {
                        continue;
                    }

                    let logit = predictions[batch_index + time_index + (label as usize)];

                    if logit <= f32::NEG_INFINITY {
                        continue;
                    }

                    let c = leaves.get_child(*b, label);
                    if !leaves[c].is_active() {
                        // Pblank(l=abcd @ t=6) = 0
                        leaves[c].new_p.blank = LogSpace(f32::NEG_INFINITY);

                        // If new child label is identical to beam label:
                        //   Plabel(l=abcc @ t=6) = Pblank(l=abc @ t=5) * P(c @ 6)
                        // Otherwise:
                        //   Plabel(l=abcd @ t=6) = P(l=abc @ t=5) * P(d @ 6)
                        let mut prev = if leaves[*b].label == label {
                            leaves[*b].old_p.blank
                        } else {
                            leaves[*b].old_p.total
                        };
                        if let Some(lm) = &language_model {
                            prev *= lm.get_probability(&leaves[*b], &leaves[c], blank_idx);
                        }
                        leaves[c].new_p.label = prev * logit;

                        // P(l=abcd @ t=6) = Plabel(l=abcd @ t=6)
                        leaves[c].new_p.total = leaves[c].new_p.label;

                        // Only insert leaf if c is better than current minimum or leaves is not full
                        if leaves[c].new_p.total > LogSpace(f32::NEG_INFINITY)
                            && (leaves.len() < beam_width
                                || leaves[c].new_p.total > leaves[leaves.min()].new_p.total)
                        {
                            if let Some(removed) = leaves.push(c) {
                                // Deactivate removed
                                leaves.deactivate(removed);
                            }
                        } else {
                            // Deactivate child
                            leaves.deactivate(c);
                        }
                    }
                }
            }
        }

        // Get maximum branch
        let final_branches = leaves.clone_heap();
        let mut best = final_branches[0];
        for j in 1..final_branches.len() {
            if leaves[final_branches[j]] > leaves[best] {
                best = final_branches[j];
            }
        }

        let seq = leaves.get_seq(best);
        outputs.push(seq);
    }

    // Get maximum output sequence length and build lengths vector
    let mut lengths = Vec::with_capacity(batch_size);
    let mut max_len = 0;
    for i in 0..outputs.len() {
        lengths.push(outputs[i].len() as i32);

        if outputs[i].len() > max_len {
            max_len = outputs[i].len();
        }
    }

    // Create sequences vector
    let mut sequences = Vec::new();
    sequences.resize(batch_size * max_len, 0);
    for i in 0..outputs.len() {
        for j in 0..outputs[i].len() {
            sequences[i * max_len + j] = outputs[i][j];
        }
    }

    DecoderOutput {
        Sequences: sequences,
        Lengths: lengths,
    }
}
