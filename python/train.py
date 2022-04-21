import argparse
from datetime import datetime
from pathlib import Path
import shutil
import torch
from torch import nn
from torch.utils import tensorboard
from torch.profiler import profile, ProfilerActivity, schedule, tensorboard_trace_handler

from data import *
from decoders import *
from metrics import *
from model import *

now_str = Path(".") / "trained" / datetime.now().strftime("%Y-%m-%dT%H:%M:%S")

parser = argparse.ArgumentParser()
parser.add_argument("--train_data_dir", "-t", type=Path, required=True,
                    help="The directory containing the training data")
parser.add_argument("--valid_data_dir", "-v", type=Path, required=True,
                    help="The directory containing the validation data")
parser.add_argument("--out_dir", "-s", type=Path, default=now_str, required=False,
                    help="The directory to save training logs and model checkpoints to. Defaults to current datetime in cwd/trained/.")
parser.add_argument("--batch_size", "-b", type=int, default=64, required=False,
                    help="The training batch size.")
parser.add_argument("--learning_rate", "-l", type=float, default=0.0001, required=False,
                    help="The training learning rate.")
parser.add_argument("--decay_rate", "-d", type=float, default=1.0, required=False,
                    help="The learning rate decay rate.")
parser.add_argument("--save_interval", "-i", type=float, default=25, required=False,
                    help="The interval (in epochs) to save network parameters at.")
parser.add_argument("--weights", "-w", type=Path, required=False,
                    help="The saved weights to initialize the model with.")

# Parse command line args
args = parser.parse_args()

# Load datasets
train_dataset = TextDataset(args.train_data_dir)
sampler = SortedRandomBatchSampler(train_dataset, batch_size=args.batch_size)
train_dataloader = torch.utils.data.DataLoader(train_dataset, batch_sampler=sampler, collate_fn=padded_collate)

valid_dataset = TextDataset(args.valid_data_dir)
sampler = SortedRandomBatchSampler(valid_dataset, batch_size=args.batch_size)
valid_dataloader = torch.utils.data.DataLoader(valid_dataset, batch_sampler=sampler, collate_fn=padded_collate)

# Create output directory for training logs and model checkpoints
args.out_dir.mkdir(parents=True, exist_ok=False)
writer = tensorboard.SummaryWriter(log_dir=args.out_dir)

# Copy dataset codec information to the output directory
shutil.copy(args.train_data_dir / "codec.json", args.out_dir / "codec.json")

# Load model and setup optimzer, loss function, decoder, accuracy metric
model = load_model(len(train_dataset.classes), args.weights)
optimizer = torch.optim.Adam(model.parameters(), lr=args.learning_rate)
lr_scheduler = torch.optim.lr_scheduler.ExponentialLR(optimizer, gamma=args.decay_rate)
ctc_loss = nn.CTCLoss(zero_infinity=False)
decoder = CTCGreedyDecoder()
accuracy_metric = SequenceAccuracy()

# Send model to appropriate device (GPU if available)
device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")
model.to(device)
print("Running on device: " + str(device))

# Save model graph to Tensorboard
sizes = torch.tensor([[1, 32, 32]]).to(device)
imgs = torch.randn((1, sizes[0, 0], sizes[0, 1], sizes[0, 2])).to(device)
writer.add_graph(model, input_to_model=(imgs, sizes))

# Begin training
epoch = 0
with profile(
    activities=[ProfilerActivity.CPU, ProfilerActivity.CUDA],
    schedule=schedule(
        skip_first=10,
        wait=2,
        warmup=2,
        active=8,
        repeat=2
    ),
    on_trace_ready=tensorboard_trace_handler(args.out_dir),
    record_shapes=True,
    profile_memory=True,
    with_stack=False
) as prof:
    while True:
        # Do training and calculate loss and accuracy
        train_loss_avg = 0.0
        train_accuracy_avg = 0.0
        for imgs, lbls, img_lens, lbl_lens in train_dataloader:
            # Send all tensors to correct device
            imgs = imgs.to(device)
            lbls = lbls.to(device)
            img_lens = img_lens.to(device)
            lbl_lens = lbl_lens.to(device)

            # Feed forward and calculate loss
            probs, prob_lens = model(imgs, img_lens)
            probs = probs.transpose(0, 1) # Make batch size come second
            prob_lens = prob_lens[:, 0]
            loss = ctc_loss(probs, lbls, prob_lens, lbl_lens)

            # Do gradient update step
            optimizer.zero_grad()
            loss.backward()
            optimizer.step()

            # Accumulate loss for this epoch
            train_loss_avg += loss.item() * imgs.size(0)

            # Decode to get output strings
            probs = probs.transpose(0, 1) # Put batch size back
            decoded, decoded_lens = decoder(probs, prob_lens)

            # Calculate accuracy
            accuracy = accuracy_metric(decoded, lbls, lbl_lens)
            train_accuracy_avg += accuracy.item() * imgs.size(0)

            # Step profiler if after first epoch (use first epoch to load all data, warmup, etc.)
            if epoch > 0:
                prof.step()

        # Step lr scheduler after every epoch
        lr_scheduler.step()

        # Calculate average loss and accuracy
        train_loss_avg /= len(train_dataloader.sampler)
        train_accuracy_avg /= len(train_dataloader.sampler)

        # Calculate loss and accuracy on validation dataset
        valid_loss_avg = 0.0
        valid_accuracy_avg = 0.0
        with torch.no_grad():
            model.train(False) # Put model in inference mode
            for imgs, lbls, img_lens, lbl_lens in valid_dataloader:
                # Send all tensors to correct device
                imgs = imgs.to(device)
                lbls = lbls.to(device)
                img_lens = img_lens.to(device)
                lbl_lens = lbl_lens.to(device)

                # Feed forward and calculate loss
                probs, prob_lens = model(imgs, img_lens)
                probs = probs.transpose(0, 1) # Make batch size come second
                prob_lens = prob_lens[:, 0]
                loss = ctc_loss(probs, lbls, prob_lens, lbl_lens)

                # Accumulate loss for this epoch
                valid_loss_avg += loss.item() * imgs.size(0)

                # Decode to get output strings
                probs = probs.transpose(0, 1) # Put batch size back
                decoded, decoded_lens = decoder(probs, prob_lens)

                # Calculate accuracy
                accuracy = accuracy_metric(decoded, lbls, lbl_lens)
                valid_accuracy_avg += accuracy.item() * imgs.size(0)

                # Step profiler if after first epoch (use first epoch to load all data, warmup, etc.)
                if epoch > 0:
                    prof.step()
            model.train(True) # Put model back in training mode

        # Calculate average loss and accuracy
        valid_loss_avg /= len(valid_dataloader.sampler)
        valid_accuracy_avg /= len(valid_dataloader.sampler)

        # Write results for epoch to Tensorboard
        writer.add_scalar("loss/train", train_loss_avg, epoch)
        writer.add_scalar("loss/validation", valid_loss_avg, epoch)
        writer.add_scalar("accuracy/train", train_accuracy_avg, epoch)
        writer.add_scalar("accuracy/validation", valid_accuracy_avg, epoch)
        writer.add_scalar("learning_rate", lr_scheduler.get_last_lr()[0], epoch)
        writer.flush()

        # Save snapshot of model parameters
        if epoch % args.save_interval == 0:
            torch.save(model.state_dict(), args.out_dir / ("epoch_" + str(epoch) + ".pt"))

        # Print results for epoch to console
        print("Epoch: {}, training loss: {}, validation loss: {}, training accuracy: {}, validation accuracy: {}".format(
            epoch, train_loss_avg, valid_loss_avg, train_accuracy_avg, valid_accuracy_avg))

        epoch += 1