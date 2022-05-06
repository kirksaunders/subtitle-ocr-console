# Subtitle OCR Console
Contained in this repository is a tool to convert image-based subtitle formats (currently only supports PGS subtitles) to a text-based subtitle format (SRT). The OCR component is a convolutional recurrent neural network (CRNN) trained with PyTorch.

## LICENSING NOTICE
The code contained within this repository is the original work of Kirk Saunders, with the exception of the `CTCBeamSearchDecoder` and `BeamEntry` classes, which were ported from TensorFlow (see those files for the original license notice). The code is currently published here without any licensing (a license has yet to be chosen). Hence, all code within this project is not to be modified, distributed, or used in any project until otherwise noted. The only usage granted currently is to clone, build, and run the application.

## Build-Time Dependencies
- .NET 6.0 Runtime + SDK (either Visual Studio or Linux command-line package)
- Rust Compiler + Cargo (must be in system PATH)

## Inference Dependencies
- CUDA (optional, for GPU inference)

## Training Dependencies
- Python 3.0
- PyTorch
- Pillow (Python package)
- CUDA (and a capable GPU, otherwise training is slow)

## Build Instructions
Depending on your platform and toolset, your build instructions vary. In general, building is done like any other C# project.

***FIRST STEP:*** Edit the project file at `subtitle-ocr-console.csproj` and change the runtime identifier to your platform (see comment in `csproj` file, and see list [here](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)).

### On Linux (Or Dotnet Command Line in General)
Assuming you have the .NET SDK installed, a self-contained executable can be built with `dotnet publish -c release --self-contained`. The produced executable is at `bin/Release/net6.0/<Runtime Identifier>/publish/subtitle-ocr-console`. This executable is completely self-contained and can be copied/moved to a separate directory for running any commands (except the Tesseract command, since it's not supported on Linux).

### With Visual Studio
To build the .NET application with Visual Studio, first ensure you have Visual Studio with .NET 6.0 SDK installed. Next, open `subtitle-ocr-console.sln` in Visual Studio. Once loaded, right click the `subtitle-ocr-console` project (NOT the solution) in the solution explorer and click `Publish`. A window will pop up asking where you would like to publish. Click through the menu choosing `Folder` for each option so that the built executable is saved locally on disk. Click `Finish` to create the Publish profile. Once that is complete, you should see the newly created profile in the publish menu. Ensure the `Configuration` is set to `Release`. Next, click the pencil next to `Target Runtime` and change the `Deployment mode` to `Self-contained`. Also change the target runtime to `win10-x64` (or whatever platform you are on, see list [here](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)). Click `Save`. Finally, click the `Publish` button in the top right of the window. You should see a green box indicating that publishing completed. Click the `Open folder` button to open the folder that the executable is contained in. This executable is completely self-contained and can be copied/moved to a separate directory for running any commands.

## Inference Usage
After building the .NET executable, there are various sub-commands that can be run. To see detailed information, run `./subtitle-ocr-console -h`. This gives information about the various sub-commands. To see information about a specific sub-command, run `./subtitle-ocr-console <sub-command> -h`.

### Convert PGS Subtitle File to SRT
The convert sub-command is used to convert a PGS subtitle to SRT. Usage is as follows:
```
./subtitle-ocr-console convert <path to PGS file> <path to output SRT file> <name of model to use>
```
There is one included pre-trained CRNN model called `eng` that can be used. This is bundled within the executable file at publish time (see above step). In fact, all trained model files included in the trained-models directory are bundled in the executable.

Also included in this directory is `example-data/spirited-away-pgs.sup`, which is a sample PGS subtitle extracted from the Blu-ray movie Spirited Away.

### Parsing and Extracting Images from a PGS File
Included is a simple command to extract all image frames from a PGS subtitle image. That can be done like so:
```
./subtitle-ocr-console parse-pgs some-pgs.sup out-image-dir/
```

## Training Usage
The full training procedure requires running the .NET application and some Python scripts.

### Codec Generation
The first step of training is to generate a codec. As always, use the `-h` flag for the .NET executable to see command details. The command that generated the codec used for the included pre-trained model is:
```
./subtitle-ocr-console generate-codec codec.json $'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz' $'0123456789' $',.!?-"\'():;' $' '
```
The dollar sign syntax is a literal string in Bash. Different shells may require different syntax to escape single or double quotes for this command.

### Data Generation
Data generation works by rendering text data as images. The text data must be sourced as plain text from files provided to the `generate-data` sub-command. The text data used for training the included pre-trained model was taken from the eBooks *Wuthering Heights* and *Moby Dick* obtained in plain text format from [Project Gutenberg](https://www.gutenberg.org/), and from various SRT subtitles taken from [Open Subtitles](https://opensubtitles.org/).

Here is an example command to generate a training dataset consisting of 100 images with one random image for every four images from the eBook:
```
./subtitle-ocr-console generate-data 100 out-dir/ codec.json some-ebook.txt another-ebook.txt
```

Generating validation data would be done the same way, except with the `-v` flag set.

**NOTE:** Validation data is generated using different fonts than training data.

### Language Model Generation
A simple character-level language model can be trained from plain-text data. The language model included with the pre-trained model was trained on the eBooks *Wuthering Heights*, *Moby Dick*, and *Dracula* obtained from [Project Gutenberg](https://www.gutenberg.org/), and from various SRT subtitles taken from [Open Subtitles](https://opensubtitles.org/).

Here is how to train a language model:
```
./subtitle-ocr-console generate-lm lm.json codec.json some-ebook.txt another-ebook.txt
```

### Model Training
To train a model, a Python script is used. Like with the .NET executable, the Python scripts offer useful help messages when the `-h` is passed to them. Here's how to train a model:
```
python python/train.py -t train-data-dir/ -v valid-data-dir -b 8
```
with a batch size of 8. Check the help message from the script to see other arguments that can be passed.

### Trained Model Exportation
To export a trained model from PyTorch to ONNX format, another Python script is used. An example of this command is:
```
python python/export.py -m trained-model-log-dir/ -e 5 -o saved-out-dir/
```
where 5 is the epoch snapshot of the trained model that you would like to export.

### Model Evaluation
To evaluation a model, the .NET executable `eval-model` command is used. Here is an example of that command in action:
```
./subtitle-ocr-console eval-model path-to-model.onnx codec.json validation-dataset-dir/ -lm lm.json
```
See the help message for the optional argument `--skip`.

### Testing Inference
To quickly test inference of the model, the `test-inference` command is used. Here is an example of that:
```
./subtitle-ocr-console test-inference trained-model.onnx codec.json -lm lm.json some-image.png another-image.png
```
where the images have already been binarized.

### Evaluating Tesseract
To evaluate Tesseract OCR, you must be using Windows. This is due to the C# bindings not bundling the Tesseract shared libraries for any platform other than Windows. Furthermore, you must run the program using `dotnet run` instead of invoking the built executable directly (the C# bindings are quite bad). You can access a developer console by going to `Tools->Command Line->Developer Powershell` in Visual Studio. An example of that command is:
```
dotnet run -- eval-tesseract tessdata-dir/ eng codec.json validation-dataset-dir/
```
where `tessdata-dir` is the directory to the tesseract data containing trained data for the `eng` model. These files can be obtained from the [Tesseract Data Repository](https://github.com/tesseract-ocr/tessdata).

### Final Model Bundling
As mentioned earlier, all final trained models included in the trained-models directory are bundled within the built .NET executable. In order to have a trained model work correctly, create a directory in trained-models with the desired name of the model. Within that directory, place the ONNX model named as `model.onnx`, the codec named as `codec.json`, and the language model named as `lm.json`. The language model is optional; PGS conversion is possible without it.
