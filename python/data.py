import json
import numpy as np
import torch
import torchvision
from torch import nn
from PIL import Image

class TextDataset(torch.utils.data.Dataset):
    def __init__(self, data_dir, augmentation=False):
        self.data_dir = data_dir
        self.augmentation = augmentation

        f = open(data_dir / "labels.json", "r")
        data = json.load(f)
        f.close()

        f = open(data_dir / "codec.json", "r")
        codec = json.load(f)
        f.close()

        self.image_extension = data["ImageExtension"]

        self.classes = ['<BLNK>']
        for char in codec:
            self.classes.append(char["Char"])
        self.class_map = {}
        for char in self.classes:
            self.class_map[char] = len(self.class_map)

        self.image_labels = data["Lines"]

        if self.augmentation:
            self.rand_translate = torchvision.transforms.RandomAffine(degrees=0, translate=(0.1, 0.0))

    def __len__(self):
        return len(self.image_labels)

    def __getitem__(self, idx):
        annotation = self.image_labels[idx]
        path = str(self.data_dir / str(annotation["Image"])) + self.image_extension

        # Load an convert image to torch tensor
        if not "LoadedImage" in annotation:
            img = Image.open(path, "r")
            assert img.height == 32
            img_data = np.asarray(img)
            img_data = img_data[:, :, -1] # use only alpha channel
            img_data = img_data.astype(np.float32) / 255.0
            img_tensor = torch.from_numpy(img_data)
            img_tensor = img_tensor.transpose(0, 1) # Make width come first
            img_tensor = img_tensor.unsqueeze(0) # Add one channel
            annotation["LoadedImage"] = img_tensor

        # Load and encode label
        if not "EncodedLabel" in annotation:
            label = torch.zeros(len(annotation["Text"]), dtype=torch.long)
            for i in range(len(annotation["Text"])):
                label[i] = self.class_map[annotation["Text"][i]]
            annotation["EncodedLabel"] = label

        # Do augmentation
        if self.augmentation:
            img = annotation["LoadedImage"]
            size = img.size()
            rand_resize = 0.85 + torch.rand(1) * 0.55
            width = int(size[1] * rand_resize + 0.5)
            out_img = torchvision.transforms.Resize((width, size[2]))(img)
            out_img = self.rand_translate(out_img)
        else:
            out_img = annotation["LoadedImage"]

        # Remove channel dimension (because pad_sequence in the batch collate fn needs it gone anyways)
        out_img = out_img.squeeze(0)

        return out_img, annotation["EncodedLabel"]

def padded_sorted_collate(batch):
    # Reverse sort by image width
    batch.sort(reverse=True, key=lambda x: x[0].size(0))

    images = [x[0] for x in batch]
    labels = [x[1] for x in batch]

    # Pad images by the max width in batch
    image_sizes = torch.tensor([x.size() for x in images], dtype=torch.long)
    padded_images = nn.utils.rnn.pad_sequence(images, batch_first=True)
    padded_images = torch.unsqueeze(padded_images, 1)
    image_sizes = torch.cat((torch.ones(len(batch), 1, dtype=torch.long), image_sizes), dim=1)

    # Pad labels by the max length in batch
    label_sizes = torch.tensor([x.size(0) for x in labels], dtype=torch.long)
    padded_labels = nn.utils.rnn.pad_sequence(labels, batch_first=True)

    return [padded_images, padded_labels, image_sizes, label_sizes]
