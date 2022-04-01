import torch

from modules import *

def load_model(num_classes, weights_path=None):
    model = SequentialMultipleInput(
        SizeTrackingConv2d(1, 64, 3, padding="same_right_bottom"),
        SizeTrackingReLU(),
        SizeTrackingMaxPool2d(2),

        SizeTrackingConv2d(64, 128, 3, padding="same_right_bottom"),
        SizeTrackingReLU(),
        SizeTrackingMaxPool2d(2),

        # Note: No need for bias when using batch normalization (it will just get cancelled out)
        SizeTrackingConv2d(128, 256, 3, padding="same_right_bottom", bias=False),
        SizeTrackingBatchNorm2d(256),
        SizeTrackingReLU(),

        SizeTrackingConv2d(256, 256, 3, padding="same_right_bottom"),
        SizeTrackingReLU(),
        SizeTrackingMaxPool2d((1, 2)),

        SizeTrackingConv2d(256, 512, 3, padding="same_right_bottom", bias=False),
        SizeTrackingBatchNorm2d(512),
        SizeTrackingReLU(),

        SizeTrackingConv2d(512, 512, 3, padding="same_right_bottom"),
        SizeTrackingReLU(),
        SizeTrackingMaxPool2d((1, 2)),

        SizeTrackingConv2d(512, 512, (1, 2), bias=False),
        SizeTrackingBatchNorm2d(512),
        SizeTrackingReLU(),

        SizeTrackingPermute(0, 2, 1, 3),
        SizeTrackingCombineDims(2, 3),

        SequencePacker(),
        SizeTrackingLSTM(input_size=512, hidden_size=256, bidirectional=True, batch_first=True),
        SizeTrackingLSTM(input_size=512, hidden_size=256, bidirectional=True, batch_first=True),
        SequenceUnpacker(),

        SizeTrackingLinear(in_features=512, out_features=num_classes),
        SizeTrackingLogSoftmax(dim=2)
    )

    if not weights_path is None:
        model.load_state_dict(torch.load(weights_path))

    return model