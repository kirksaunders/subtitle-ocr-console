import torch
from torch import nn

class SequentialMultipleInput(nn.Sequential):
    def forward(self, *input):
        for module in self:
            input = module(*input)
        return input

class SizeTracking(nn.Module):
    def __init__(self):
        super(SizeTracking, self).__init__()

    def forward(self, x, sizes):
        return self.layer(x), self._calculate_sizes(sizes)

class SizeTrackingConv2d(nn.Module):
    def __init__(self, *args, **kwargs):
        super(SizeTrackingConv2d, self).__init__()

        # Handle special padding manually because conversion to ONNX doesn't support it
        if len(args) > 4 and isinstance(args[4], str):
            self.padding = args[4]
            args = args[:4] + (0,) + args[5:]
        elif "padding" in kwargs and not kwargs["padding"] is None:
            self.padding = kwargs["padding"]
            kwargs["padding"] = 0
        else:
            self.padding = 0

        self.layer = nn.Conv2d(*args, **kwargs)

        self._init_padding()

    # Source: https://discuss.pytorch.org/t/same-padding-equivalent-in-pytorch/85121/9
    def _init_padding(self):
        if self.padding == "same":
            raise ValueError("Same padding not currently supported")
        elif self.padding == "valid":
            raise ValueError("Valid padding not currently supported")
        elif self.padding == "same_right_bottom":
            padding_vert = self.layer.dilation[0] * (self.layer.kernel_size[0] - 1)
            padding_horz = self.layer.dilation[1] * (self.layer.kernel_size[1] - 1)

            self.padding_internal = (0, padding_horz, 0, padding_vert)
        else:
            self.padding_internal = None

    def _calculate_sizes(self, sizes):
        batch_size = sizes.size(0)
        channels = sizes.new_ones((batch_size, 1)) * self.layer.out_channels
        if self.padding == "same" or self.padding == "same_right_bottom":
            heights = sizes[:, 1]
            widths = sizes[:, 2]
        else:
            heights = torch.div(sizes[:, 1] + 2 * self.layer.padding[0] - self.layer.dilation[0] * (self.layer.kernel_size[0] - 1) - 1, self.layer.stride[0], rounding_mode="floor") + 1
            widths = torch.div(sizes[:, 2] + 2 * self.layer.padding[1] - self.layer.dilation[1] * (self.layer.kernel_size[1] - 1) - 1, self.layer.stride[1], rounding_mode="floor") + 1
        heights = heights.reshape((batch_size, 1))
        widths = widths.reshape((batch_size, 1))
        return torch.cat((channels, heights, widths), dim=1)

    def _padding(self, x):
        if not self.padding_internal is None:
            return torch.nn.functional.pad(x, self.padding_internal)
        else:
            return x

    def forward(self, x, sizes):
        return self.layer(self._padding(x)), self._calculate_sizes(sizes)

class SizeTrackingMaxPool2d(SizeTracking):
    def __init__(self, *args, **kwargs):
        super(SizeTrackingMaxPool2d, self).__init__()
        self.layer = nn.MaxPool2d(*args, **kwargs)

        # Ensure all parameters are tuples instead of single ints
        self.kernel_size = (self.layer.kernel_size, self.layer.kernel_size) if type(self.layer.kernel_size) is int else self.layer.kernel_size
        self.stride = (self.layer.stride, self.layer.stride) if type(self.layer.stride) is int else self.layer.stride
        self.padding = (self.layer.padding, self.layer.padding) if type(self.layer.padding) is int else self.layer.padding
        self.dilation = (self.layer.dilation, self.layer.dilation) if type(self.layer.dilation) is int else self.layer.dilation

    def _calculate_sizes(self, sizes):
        batch_size = sizes.size(0)
        channels = sizes[:, 0].reshape((batch_size, 1))
        # TODO: Need to implement ceil_mode=True
        heights = torch.div(sizes[:, 1] + 2 * self.padding[0] - self.dilation[0] * (self.kernel_size[0] - 1) - 1, self.stride[0], rounding_mode="floor") + 1
        widths = torch.div(sizes[:, 2] + 2 * self.padding[1] - self.dilation[1] * (self.kernel_size[1] - 1) - 1, self.stride[1], rounding_mode="floor") + 1
        heights = heights.reshape((batch_size, 1))
        widths = widths.reshape((batch_size, 1))
        return torch.cat((channels, heights, widths), dim=1)

class SizeTrackingFlatten(SizeTracking):
    def __init__(self, *args, **kwargs):
        super(SizeTrackingFlatten, self).__init__()
        self.layer = nn.Flatten(*args, **kwargs)

    def _calculate_sizes(self, sizes):
        return torch.prod(sizes, dim=1, keepdim=True)

class SizeTrackingSqueeze(nn.Module):
    def __init__(self, dim):
        super(SizeTrackingSqueeze, self).__init__()
        self.dim = dim

    def _calculate_sizes(self, sizes):
        left = sizes[:, :self.dim-1]
        right = sizes[:, self.dim:]
        return torch.cat((left, right), dim=1)

    def forward(self, x, sizes):
        assert x.size(self.dim) == 1
        return x.squeeze(dim=self.dim), self._calculate_sizes(sizes)

class SizeTrackingPermute(nn.Module):
    def __init__(self, *permutation):
        super(SizeTrackingPermute, self).__init__()
        assert permutation[0] == 0 # Don't allow permutation of batch position
        self.permutation = permutation
        self.inner_permutation = torch.tensor(permutation, dtype=torch.int64)[1:] - 1

    def _calculate_sizes(self, sizes):
        return sizes[:, self.inner_permutation]

    def forward(self, x, sizes):
        return x.permute(self.permutation), self._calculate_sizes(sizes)

class SizeTrackingCombineDims(nn.Module):
    def __init__(self, start_dim, end_dim):
        super(SizeTrackingCombineDims, self).__init__()
        assert start_dim > 0 # Don't allow combining with batch size dim
        assert end_dim > start_dim
        self.start_dim = start_dim
        self.end_dim = end_dim

    def _calculate_sizes(self, sizes):
        batch_size = sizes.size(0)
        left = sizes[:, :self.start_dim-1]
        middle = torch.torch.prod(sizes[:, self.start_dim-1:self.end_dim], dim=1)
        middle = middle.reshape((batch_size, 1))
        right = sizes[:, self.end_dim:]
        return torch.cat((left, middle, right), dim=1)

    def forward(self, x, sizes):
        #return x.flatten(start_dim=self.start_dim, end_dim=self.end_dim), self._calculate_sizes(sizes)
        return x.flatten(start_dim=2, end_dim=3), sizes

class SequencePacker(nn.Module):
    def __init__(self):
        super(SequencePacker, self).__init__()

    def forward(self, x, sizes):
        # Note the lengths tensor has to be on the cpu
        return torch.nn.utils.rnn.pack_padded_sequence(x, sizes[:, 0].to("cpu"), batch_first=True), sizes

class SequenceUnpacker(nn.Module):
    def __init__(self, padding_value=0):
        super(SequenceUnpacker, self).__init__()
        self.padding_value = padding_value

    def forward(self, x, sizes):
        out, _ = torch.nn.utils.rnn.pad_packed_sequence(x, padding_value=self.padding_value, batch_first=True)
        return out, sizes

class SizeTrackingLSTM(nn.Module):
    def __init__(self, *args, **kwargs):
        super(SizeTrackingLSTM, self).__init__()
        self.layer = nn.LSTM(*args, **kwargs)
        assert self.layer.batch_first == True # Require batch first
        self.h_out = self.layer.proj_size if self.layer.proj_size > 0 else self.layer.hidden_size
        if self.layer.bidirectional:
            self.h_out *= 2

    def _calculate_sizes(self, sizes):
        batch_size = sizes.size(0)
        seq_lens = sizes[:, 0].reshape((batch_size, 1))
        h_out = sizes.new_ones((batch_size, 1)) * self.h_out
        return torch.cat((seq_lens, h_out), dim=1)

    def forward(self, x, sizes):
        out, _ = self.layer(x)
        return out, self._calculate_sizes(sizes)

class SizeTrackingSoftmax(SizeTracking):
    def __init__(self, *args, **kwargs):
        super(SizeTrackingSoftmax, self).__init__()
        self.layer = nn.Softmax(*args, **kwargs)

    def _calculate_sizes(self, sizes):
        return sizes

class SizeTrackingReLU(SizeTracking):
    def __init__(self, *args, **kwargs):
        super(SizeTrackingReLU, self).__init__()
        self.layer = nn.ReLU(*args, **kwargs)

    def _calculate_sizes(self, sizes):
        return sizes

class SizeTrackingLogSoftmax(SizeTracking):
    def __init__(self, *args, **kwargs):
        super(SizeTrackingLogSoftmax, self).__init__()
        self.layer = nn.LogSoftmax(*args, **kwargs)

    def _calculate_sizes(self, sizes):
        return sizes

class SizeTrackingLinear(SizeTracking):
    def __init__(self, *args, **kwargs):
        super(SizeTrackingLinear, self).__init__()
        self.layer = nn.Linear(*args, **kwargs)

    def _calculate_sizes(self, sizes):
        batch_size = sizes.size(0)
        left = sizes[:, :-1]
        right = sizes.new_ones((batch_size, 1)) * self.layer.out_features
        return torch.cat((left, right), dim=1)

class SizeTrackingBatchNorm2d(SizeTracking):
    def __init__(self, *args, **kwargs):
        super(SizeTrackingBatchNorm2d, self).__init__()
        self.layer = nn.BatchNorm2d(*args, **kwargs)

    def _calculate_sizes(self, sizes):
        return sizes
