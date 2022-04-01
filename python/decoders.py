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
        
        # Calculate output lenghts
        out_lengths = lengths.new_tensor([x.size(0) for x in out])

        # Pack into one tensor
        out = nn.utils.rnn.pad_sequence(out, batch_first=True)

        return out, out_lengths