import json
import numpy as np
import torch
from torch import nn
from PIL import Image

class TextDataset(torch.utils.data.Dataset):
    def __init__(self, data_dir):
        self.data_dir = data_dir

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
            annotation["LoadedImage"] = img_tensor

        # Load and encode label
        if not "EncodedLabel" in annotation:
            label = torch.zeros(len(annotation["Text"]), dtype=torch.long)
            for i in range(len(annotation["Text"])):
                label[i] = self.class_map[annotation["Text"][i]]
            annotation["EncodedLabel"] = label

        return annotation["LoadedImage"], annotation["EncodedLabel"]

class SortedRandomBatchSampler(torch.utils.data.Sampler):
    def __init__(self, data_source, batch_size=None, generator=None):
        self.data_source = data_source
        self.batch_size = batch_size
        self.generator = generator

        if not isinstance(self.batch_size, int) or self.batch_size <= 0:
            raise ValueError("batch_size should be a positive integer "
                             "value, but got batch_size={}".format(self.batch_size))

    def _sort(self, indices):
        return sorted(indices, key=lambda x: self.data_source[x][0].size(0), reverse=True)

    def __iter__(self):
        n = len(self.data_source)
        if self.generator is None:
            seed = int(torch.empty((), dtype=torch.int64).random_().item())
            generator = torch.Generator()
            generator.manual_seed(seed)
        else:
            generator = self.generator

        perm = torch.randperm(n, generator=generator)

        for i in range(0, n, self.batch_size):
            yield self._sort(perm[i:min(n, i+self.batch_size)].tolist())

    def __len__(self):
        return len(self.data_source)

def padded_collate(batch):
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
