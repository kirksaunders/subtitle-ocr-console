import argparse
from datetime import datetime
from pathlib import Path
import shutil
import torch
from torch import nn
from torch.utils import tensorboard

from data import *
from decoders import *
from metrics import *
from model import *
from trainer import *

if __name__ == '__main__':
    now_str = Path(".") / "trained" / datetime.now().strftime("%Y-%m-%dT%H:%M:%S")

    parser = argparse.ArgumentParser()
    parser.add_argument("--train_data_dir", "-t", type=Path, required=True,
                        help="The directory containing the training data")
    parser.add_argument("--valid_data_dir", "-v", type=Path, required=True,
                        help="The directory containing the validation data")
    parser.add_argument("--out_dir", "-o", type=Path, default=now_str, required=False,
                        help="The directory to save training logs and model checkpoints to. Defaults to current datetime in cwd/trained/.")
    parser.add_argument("--batch_size", "-b", type=int, default=8, required=False,
                        help="The training batch size.")
    parser.add_argument("--learning_rate", "-l", type=float, default=0.0001, required=False,
                        help="The training learning rate.")
    parser.add_argument("--decay_rate", "-d", type=float, default=1.0, required=False,
                        help="The learning rate decay rate.")
    parser.add_argument("--epoch_size", "-e", type=int, required=False,
                        help="The epoch size (in number of batches) used to log to tensorboard and for model checkpoints. If not supplied, full dataset is one epoch.")
    parser.add_argument("--save_interval", "-s", type=int, default=5, required=False,
                        help="The interval (in number of epochs) to save model checkpoint.")
    parser.add_argument("--weights", "-w", type=Path, required=False,
                        help="The saved weights to initialize the model with.")

    # Parse command line args
    args = parser.parse_args()

    # Load datasets
    train_dataset = TextDataset(args.train_data_dir, augmentation=True)
    train_dataloader = torch.utils.data.DataLoader(train_dataset, batch_size=args.batch_size, shuffle=True, collate_fn=padded_sorted_collate, num_workers=4, pin_memory=True)

    valid_dataset = TextDataset(args.valid_data_dir, augmentation=False)
    valid_dataloader = torch.utils.data.DataLoader(valid_dataset, batch_size=args.batch_size, collate_fn=padded_sorted_collate, num_workers=2, pin_memory=True)

    # Create output directory for training logs and model checkpoints
    args.out_dir.mkdir(parents=True, exist_ok=False)
    log_writer = tensorboard.SummaryWriter(log_dir=args.out_dir)

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
    log_writer.add_graph(model, input_to_model=(imgs, sizes))

    # Begin training
    trainer = CTCTrainer(model, args.out_dir, device, train_dataloader, valid_dataloader, optimizer, lr_scheduler, log_writer)
    trainer.train(args.save_interval, args.epoch_size)
