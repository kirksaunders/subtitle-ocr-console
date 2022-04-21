import argparse
import json
from pathlib import Path
import shutil
import torch

from model import *

parser = argparse.ArgumentParser()
parser.add_argument("--model_dir", "-m", type=Path, required=True,
                    help="The directory containing the trained model")
parser.add_argument("--epoch", "-e", type=int, required=True,
                    help="The epoch of the saved weights.")
parser.add_argument("--out_dir", "-s", type=Path, required=True,
                    help="The path to save the exported model to.")

# Parse command line args
args = parser.parse_args()

# Create output directory for exported model
args.out_dir.mkdir(parents=True, exist_ok=False)

# Copy model codec information to the output directory
shutil.copy(args.model_dir / "codec.json", args.out_dir / "codec.json")

# Get model codec information
f = open(args.model_dir / "codec.json", "r")
codec = json.load(f)
f.close()

# Load model and put it in eval mode
model = load_model(len(codec) + 1, args.model_dir / ("epoch_" + str(args.epoch) + ".pt"))
model.eval()

# Export model
x = torch.ones((1, 1, 4, 32))
l = torch.tensor([[1, 4, 32]])

torch.onnx.export(model, (x, l), args.out_dir / "model.onnx",
                  input_names=["images", "sizes"],
                  output_names=["predictions", "sizes_out"],
                  dynamic_axes={
                      "images": {0: "batch_size", 2: "img_width"},
                      "sizes": {0: "batch_size"},
                  }, opset_version=9)