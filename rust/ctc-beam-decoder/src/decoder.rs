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
