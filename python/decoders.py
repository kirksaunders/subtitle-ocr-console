import bisect
import numpy as np
import torch
from torch import nn

class CTCGreedyDecoder(nn.Module):
    def __init__(self):
        super(CTCGreedyDecoder, self).__init__()

    def forward(self, probabilities, lengths):
        # Reduce to max class at each prediction
        out = torch.argmax(probabilities, dim=-1)

        # Squash adjacent duplicates
        out = [torch.unique_consecutive(x[:y]) for x, y in zip(out, lengths)]

        # Remove blank predictions
        out = [probabilities.new_tensor([i for i in x.tolist() if i != 0], dtype=torch.long) for x in out]
        
        # Calculate output lengths
        out_lengths = lengths.new_tensor([x.size(0) for x in out])

        # Pack into one tensor
        out = nn.utils.rnn.pad_sequence(out, batch_first=True)

        return out, out_lengths

with np.errstate(divide='ignore'):
    def logaddexp(x, y):
        return np.logaddexp(x, y)

with np.errstate(divide='ignore'):
    LOG_0 = np.log(0.0)
    LOG_1 = np.log(1.0)

class BeamProbability():
    def __init__(self):
        self.reset()

    def reset(self):
        self.total = LOG_0
        self.blank = LOG_0
        self.label = LOG_0

    def copy(self):
        c = BeamProbability()
        c.total = self.total
        c.blank = self.blank
        c.label = self.label

        return c

class BeamEntry():
    def __init__(self, parent, label):
        self.parent = parent
        self.label = label
        self.children = {}

        self.oldp = BeamProbability()
        self.newp = BeamProbability()

    def __lt__(self, other):
        return self.newp.total > other.newp.total

    def active(self):
        return np.not_equal(self.newp.total, LOG_0)

    def get_child(self, label):
        if not label in self.children:
            child = BeamEntry(self, label)
            self.children[label] = child

            return child
        else:
            return self.children[label]

    def label_seq(self):
        seq = [self.label]
        parent = self.parent
        while not parent is None:
            seq.insert(0, parent.label)
            parent = parent.parent

        return seq

class CTCBeamDecoder(nn.Module):
    def __init__(self):
        super(CTCBeamDecoder, self).__init__()

    def forward(self, probabilities, lengths, beam_width=1, blank_index=0):
        # Reduce to max class at each prediction

        device = probabilities.device

        probabilities = probabilities.detach().cpu().numpy()
        lengths = lengths.detach().cpu().numpy()

        batch_size = probabilities.shape[0]
        num_labels = probabilities.shape[2]

        decoded = []
        for i in range(batch_size):
            root = BeamEntry(None, blank_index)
            root.newp.total = LOG_1
            root.newp.blank = LOG_1
            leaves = [root]

            for t in range(lengths[i]):
                probs = probabilities[i, t, :]

                # Extract beams sorted in decreasing new probability
                branches = leaves.copy()
                leaves.clear()

                # Move branch probabilities forward a time step
                for b in branches:
                    b.oldp = b.newp.copy()

                for b in branches:
                    if not b.parent is None: # if not the root
                        if b.parent.active:
                            # If last two sequence characters are identical:
                            #   Plabel(l=acc @ t=6) = (Plabel(l=acc @ t=5)
                            #                          + Pblank(l=ac @ t=5))
                            # else:
                            #   Plabel(l=abc @ t=6) = (Plabel(l=abc @ t=5)
                            #                          + P(l=ab @ t=5))
                            prev = b.parent.oldp.blank if b.label == b.parent.label else b.parent.oldp.total
                            # TODO: Apply language model here (add to prev)
                            b.newp.label = logaddexp(b.newp.label, prev)
                        
                        # Plabel(l=abc @ t=6) *= P(c @ 6)
                        b.newp.label += probs[b.label]
                    
                    # Pblank(l=abc @ t=6) = P(l=abc @ t=5) * P(- @ 6)
                    b.newp.blank = b.oldp.total + probs[blank_index]
                    # P(l=abc @ t=6) = Plabel(l=abc @ t=6) + Pblank(l=abc @ t=6)
                    b.newp.total = logaddexp(b.newp.blank, b.newp.label)

                    # Push the entry back to the top paths list
                    bisect.insort(leaves, b)

                # A new leaf (represented by its BeamProbability) is a candidate
                # iff its total probability is nonzero and either the beam list
                # isn't full, or the lowest probability entry in the beam has a
                # lower probability than the leaf.
                is_candidate = lambda prob: prob.total > LOG_0 and (len(leaves) < beam_width or prob.total > leaves[-1].newp.total)

                # Grow new leaves
                for b in branches:
                    if not is_candidate(b.oldp):
                        continue

                    for label in range(num_labels):
                        # Blank character case already handled above
                        if label == blank_index:
                            continue

                        logit = probs[label]

                        if logit <= -9:
                            continue

                        c = b.get_child(label)
                        if not c.active():
                            #   Pblank(l=abcd @ t=6) = 0
                            c.newp.blank = LOG_0
                            # If new child label is identical to beam label:
                            #   Plabel(l=abcc @ t=6) = Pblank(l=abc @ t=5) * P(c @ 6)
                            # Otherwise:
                            #   Plabel(l=abcd @ t=6) = P(l=abc @ t=5) * P(d @ 6)
                            # TODO: Language model here?
                            prev = b.oldp.blank if c.label == b.label else b.oldp.total
                            # TODO: And also LM here (add to prev)
                            c.newp.label = logit + prev

                            # P(l=abcd @ t=6) = Plabel(l=abcd @ t=6)
                            c.newp.total = c.newp.label

                            if is_candidate(c.newp):
                                # Before adding the new node to the beam, check if the beam
                                # is already at maximum width.
                                if len(leaves) == beam_width:
                                    # Bottom is no longer in the beam search. Reset
                                    # its probability; signal it's no longer in the beam search.
                                    leaves[-1].newp.reset()
                                    del leaves[-1]

                                # Finally, add c to leaves
                                bisect.insort(leaves, c)
                            else:
                                # Deactivate child
                                c.oldp.reset()
                                c.newp.reset()
                        
            best = leaves[0]
            decoded.append(torch.as_tensor(best.label_seq()[1:], device=device, dtype=torch.long))
        
        # Calculate output lengths
        decoded_lengths = torch.as_tensor([x.size(0) for x in decoded], device=device, dtype=torch.long)

        # Pack into one tensor
        decoded = nn.utils.rnn.pad_sequence(decoded, batch_first=True)

        return decoded, decoded_lengths
