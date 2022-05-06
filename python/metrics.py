import torch
from torch import nn

class SequenceAccuracy(nn.Module):
    def __init__(self):
        super(SequenceAccuracy, self).__init__()

    def forward(self, predictions, labels, label_lengths):
        # Pad tensors to maximum length
        pred_len = predictions.size(1)
        label_len = labels.size(1)
        max_len = torch.max(predictions.new_tensor([pred_len, label_len]))
        predictions = nn.functional.pad(predictions, (0, max_len - pred_len))
        labels = nn.functional.pad(labels, (0, max_len - label_len))

        # Create equality matrix
        equal = torch.eq(predictions, labels).int()
        
        # Count elements based on label padding mask
        mask = torch.ne(labels, 0)
        num_equal = torch.sum(torch.masked_select(equal, mask))

        # Count number of total characters in labels
        num_chars = torch.sum(label_lengths)

        return num_equal.double() / num_chars.double()