import argparse
import json
from pathlib import Path
from PIL import Image
import torch
from torch import nn

import os
#os.environ["TF_CPP_MIN_LOG_LEVEL"] = "2"
#os.environ["CUDA_VISIBLE_DEVICES"] = '-1'
#import tensorflow as tf

from decoders import *
from metrics import *
from model import *

parser = argparse.ArgumentParser()
parser.add_argument("--model_dir", "-m", type=Path, required=True,
                    help="The directory containing the trained model")
parser.add_argument("--epoch", "-e", type=int, required=True,
                    help="The epoch of the saved weights.")

# Parse command line args
args = parser.parse_args()

# Get model codec information
f = open(args.model_dir / "codec.json", "r")
codec = json.load(f)
f.close()

# Load model and put it in eval mode
model = load_model(len(codec) + 1, args.model_dir / ("epoch_" + str(args.epoch) + ".pt"))
model.eval()
greedy_decoder = CTCGreedyDecoder()
beam_decoder = CTCBeamDecoder()

# Send model to appropriate device (GPU if available)
device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")
model.to(device)
print("Running on device: " + str(device))

img = Image.open("out/test.png", "r")
assert img.height == 32
img_data = np.asarray(img)
img_data = img_data[:, :, -1] # use only alpha channel
img_data = img_data.astype(np.float32) / 255.0
img_tensor = torch.from_numpy(img_data)
img_tensor = img_tensor.transpose(0, 1) # Make width come first

images = [img_tensor]
image_sizes = torch.tensor([x.size() for x in images], dtype=torch.long)
padded_images = nn.utils.rnn.pad_sequence(images, batch_first=True)
padded_images = torch.unsqueeze(padded_images, 1)
image_sizes = torch.cat((torch.ones(len(images), 1, dtype=torch.long), image_sizes), dim=1)

padded_images = padded_images.to(device)
image_sizes = image_sizes.to(device)

probs, prob_lens = model(padded_images, image_sizes)
prob_lens = prob_lens[:, 0]

""" probs = torch.tensor([[[0.8, 0.2, 0.0], [0.6, 0.4, 0.0]]])
probs = torch.log(probs)
prob_lens = torch.tensor([2])
classes = ['<BLNK>', 'a', 'b'] """

""" decoded, decoded_lens = greedy_decoder(probs, prob_lens)

for i in range(decoded.size(0)):
    out = ""
    for j in range(decoded_lens[i]):
        out += classes[decoded[i, j]]
    print(out) """

""" from line_profiler import LineProfiler

lp = LineProfiler()
w = lp(DECODE)
decoded, decoded_lens = w(probs, prob_lens, 100)
lp.print_stats() """

decoded, decoded_lens = beam_decoder(probs, prob_lens, 100)

for i in range(decoded.size(0)):
    out = ""
    for j in range(decoded_lens[i]):
        out += codec[decoded[i, j] - 1]["Char"]
    print(out)

""" probs = probs.transpose(0, 1) # Make batch size come second
probs = probs.detach().cpu().numpy()
prob_lens = prob_lens.detach().cpu().numpy()
probs[:, :, :] = np.concatenate((probs[:, :, 1:], probs[:, :, 0:1]), axis=-1)

decoded, _ = tf.nn.ctc_beam_search_decoder(probs, prob_lens, 100, 1)
#decoded, _ = tf.nn.ctc_greedy_decoder(probs, prob_lens)

for i in range(len(decoded)):
    out = ""
    dense = tf.sparse.to_dense(decoded[i])
    for j in range(dense.shape[1]):
        idx = dense[0, j] + 1
        if idx >= len(classes):
            idx = 0
        out += classes[idx]
    print(out) """
