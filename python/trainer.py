import torch
from torch import nn
from torch.profiler import profile, ProfilerActivity, schedule, tensorboard_trace_handler
from tqdm import tqdm

from decoders import *
from metrics import *

class CTCTrainer:
    def __init__(self, model, out_dir, device, train_dataloader, valid_dataloader, optimizer, lr_scheduler, log_writer):
        self.model = model
        self.out_dir = out_dir
        self.device = device
        self.train_dataloader = train_dataloader
        self.valid_dataloader = valid_dataloader
        self.optimizer = optimizer
        self.lr_scheduler = lr_scheduler
        self.log_writer = log_writer

        self.ctc_loss = nn.CTCLoss(zero_infinity=False)
        self.decoder = CTCGreedyDecoder()
        self.accuracy_metric = SequenceAccuracy()

    def _save(self):
        # Save snapshot of model parameters
        torch.save(self.model.state_dict(), self.out_dir / (f"epoch_{self.epoch}.pt"))

    def _validate(self):
        # Calculate loss and accuracy on validation dataset
        with torch.no_grad():
            with tqdm(self.valid_dataloader, unit="batch") as tepoch:
                tepoch.set_description(f"Epoch {self.epoch} Validation")

                self.model.train(False) # Put model in inference mode
                self.valid_loss_avg = 0.0
                self.valid_accuracy_avg = 0.0
                self.valid_count = 0
                for imgs, lbls, img_lens, lbl_lens in tepoch:
                    # Send all tensors to correct device
                    imgs = imgs.to(self.device)
                    lbls = lbls.to(self.device)
                    img_lens = img_lens.to(self.device)
                    lbl_lens = lbl_lens.to(self.device)

                    # Feed forward and calculate loss
                    probs, prob_lens = self.model(imgs, img_lens)
                    probs = probs.transpose(0, 1) # Make batch size come second
                    prob_lens = prob_lens[:, 0]
                    loss = self.ctc_loss(probs, lbls, prob_lens, lbl_lens)

                    # Accumulate loss for this epoch
                    self.valid_loss_avg += loss.item() * imgs.size(0)

                    # Decode to get output strings
                    probs = probs.transpose(0, 1) # Put batch size back
                    decoded, decoded_lens = self.decoder(probs, prob_lens)

                    # Calculate accuracy
                    accuracy = self.accuracy_metric(decoded, lbls, lbl_lens)
                    self.valid_accuracy_avg += accuracy.item() * imgs.size(0)

                    # Update information in progress bar
                    self.valid_count += imgs.size(0)
                    tepoch.set_postfix(loss=(self.valid_loss_avg / self.valid_count), accuracy=(self.valid_accuracy_avg / self.valid_count))

                self.model.train(True) # Put model back in training mode

        self._log_validation()

    def _log_training(self):
        # Write training results for epoch to Tensorboard
        self.log_writer.add_scalar("loss/train", self.train_loss_avg / self.train_count, self.epoch)
        self.log_writer.add_scalar("accuracy/train", self.train_accuracy_avg / self.train_count, self.epoch)
        self.log_writer.add_scalar("learning_rate", self.lr_scheduler.get_last_lr()[0], self.epoch)
        self.log_writer.flush()

    def _log_validation(self):
        # Write validation results for epoch to Tensorboard
        self.log_writer.add_scalar("loss/validation", self.valid_loss_avg / self.valid_count, self.epoch)
        self.log_writer.add_scalar("accuracy/validation", self.valid_accuracy_avg / self.valid_count, self.epoch)
        self.log_writer.flush()

    def _train_step(self, imgs, lbls, img_lens, lbl_lens):
        # Send all tensors to correct device
        imgs = imgs.to(self.device)
        lbls = lbls.to(self.device)
        img_lens = img_lens.to(self.device)
        lbl_lens = lbl_lens.to(self.device)

        # Feed forward and calculate loss
        probs, prob_lens = self.model(imgs, img_lens)
        probs = probs.transpose(0, 1) # Make batch size come second
        prob_lens = prob_lens[:, 0]
        loss = self.ctc_loss(probs, lbls, prob_lens, lbl_lens)

        # Do gradient update step
        self.optimizer.zero_grad()
        loss.backward()
        self.optimizer.step()

        # Accumulate loss for this epoch
        self.train_loss_avg += loss.item() * imgs.size(0)

        # Decode to get output strings
        probs = probs.transpose(0, 1) # Put batch size back
        decoded, decoded_lens = self.decoder(probs, prob_lens)

        # Calculate accuracy
        accuracy = self.accuracy_metric(decoded, lbls, lbl_lens)
        self.train_accuracy_avg += accuracy.item() * imgs.size(0)

    def train(self, save_interval, epoch_size=None, num_epochs=None):
        self.epoch = 1
        
        with profile(
            activities=[ProfilerActivity.CPU, ProfilerActivity.CUDA],
            schedule=schedule(
                skip_first=10,
                wait=2,
                warmup=2,
                active=8,
                repeat=2
            ),
            on_trace_ready=tensorboard_trace_handler(self.out_dir),
            record_shapes=True,
            profile_memory=True,
            with_stack=False
        ) as prof:
            while num_epochs is None or self.epoch < num_epochs + 1:
                self.train_loss_avg = 0.0
                self.train_accuracy_avg = 0.0
                self.train_count = 0

                sub_epoch = 0
                batch_number = 0
                pbar = None

                for imgs, lbls, img_lens, lbl_lens in self.train_dataloader:
                    if not epoch_size is None:
                        s_epoch = batch_number // epoch_size
                        if sub_epoch != s_epoch:
                            # Delete pbar
                            pbar.close()
                            pbar = None

                            self._log_training()
                            self._validate()

                            # Step lr scheduler after every epoch
                            self.lr_scheduler.step()

                            # End of last sub-epoch, do we save?
                            if self.epoch % save_interval == 0:
                                self._save()

                            sub_epoch = s_epoch
                            self.epoch += 1

                            # Reset loss and accuracy counters and count
                            self.train_loss_avg = 0.0
                            self.train_accuracy_avg = 0.0
                            self.train_count = 0

                    if pbar is None:
                        if not epoch_size is None:
                            sub_epoch_len = min(epoch_size, len(self.train_dataloader) - batch_number)
                            pbar = tqdm(total=sub_epoch_len, unit="batch")
                        else:
                            pbar = tqdm(total=len(self.train_dataloader), unit="batch")
                        # Extra two spaces are to make it align with validation
                        pbar.set_description(f"Epoch {self.epoch} Training  ")

                    self._train_step(imgs, lbls, img_lens, lbl_lens)

                    # Update information in progress bar
                    self.train_count += imgs.size(0)
                    batch_number += 1
                    pbar.set_postfix(loss=(self.train_loss_avg / self.train_count), accuracy=(self.train_accuracy_avg / self.train_count))
                    pbar.update(1)

                    # Step profiler if after first epoch (use first epoch to warmup)
                    if self.epoch > 1:
                        prof.step()

                # Close pbar
                pbar.close()

                self._log_training()
                self._validate()

                # Step lr scheduler after every epoch
                self.lr_scheduler.step()

                if self.epoch % save_interval == 0:
                    self._save()

                self.epoch += 1

