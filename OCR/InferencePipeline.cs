using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace subtitle_ocr_console.OCR;

public class InferencePipeline
{
    private readonly InferenceModel _model;
    private readonly TaskScheduler _inputScheduler;
    private readonly TaskScheduler _modelScheduler;
    private readonly TaskScheduler _decodeScheduler;

    public InferencePipeline(InferenceModel model, TaskScheduler inputScheduler, TaskScheduler modelScheduler, TaskScheduler decodeScheduler)
    {
        _model = model;
        _inputScheduler = inputScheduler;
        _modelScheduler = modelScheduler;
        _decodeScheduler = decodeScheduler;
    }

    public Task<List<string>> Process(List<Image<A8>> images, LanguageModel? languageModel = null)
    {
        var inputTaskBase = InputTask(images);
        var modelTaskBase = ModelTask(inputTaskBase);

        var batchSize = images.Count;
        var decodeTask = DecodeTask(modelTaskBase, languageModel);

        var modelTask = new Task(() =>
        {
            // Run synchronously
            modelTaskBase.RunSynchronously();

            // Start decode task when done
            decodeTask.Start(_decodeScheduler);
        });

        var inputTask = new Task(() =>
        {
            // Run synchronously
            inputTaskBase.RunSynchronously();

            // Start model task when done
            modelTask.Start(_modelScheduler);
        });

        // Start input task (may block if _inputScheduler's queue is full)
        inputTask.Start(_inputScheduler);

        return decodeTask;
    }

    public List<Task<List<string>>> Process(List<List<Image<A8>>> imageBatches, LanguageModel? languageModel = null)
    {
        var inputTaskBase = InputTask(imageBatches);
        var modelTaskBase = ModelTask(inputTaskBase);

        var decodeTasks = new List<Task<List<string>>>();
        var batchIndex = 0;
        foreach (var batch in imageBatches)
        {
            var batchSize = batch.Count;
            var batchIndexCopy = batchIndex;
            var decodeTask = DecodeTask(modelTaskBase, batchSize, batchIndexCopy, languageModel);

            batchIndex += batchSize;
            decodeTasks.Add(decodeTask);
        }

        var modelTask = new Task(() =>
        {
            // Run synchronously
            modelTaskBase.RunSynchronously();

            // Start decode tasks when done
            foreach (var decodeTask in decodeTasks)
            {
                decodeTask.Start(_decodeScheduler);
            }
        });

        var inputTask = new Task(() =>
        {
            // Run synchronously
            inputTaskBase.RunSynchronously();

            // Start model task when done
            modelTask.Start(_modelScheduler);
        });

        // Start input task (may block if _inputScheduler's queue is full)
        inputTask.Start(_inputScheduler);

        return decodeTasks;
    }

    private Task<List<NamedOnnxValue>> InputTask(List<Image<A8>> images)
    {
        return new(() => _model.PrepareInput(images));
    }

    private Task<List<NamedOnnxValue>> InputTask(List<List<Image<A8>>> imageBatches)
    {
        return new(() =>
        {
            // Flatten images into single list
            var images = new List<Image<A8>>();
            foreach (var batch in imageBatches)
            {
                foreach (var image in batch)
                {
                    images.Add(image);
                }
            }

            return _model.PrepareInput(images);
        });
    }

    private Task<(Tensor<float>, Tensor<int>)> ModelTask(Task<List<NamedOnnxValue>> inputTask)
    {
        return new(() =>
        {
            inputTask.Wait();

            var input = inputTask.Result;

            return _model.FeedForward(input);
        });
    }

    private Task<List<string>> DecodeTask(Task<(Tensor<float>, Tensor<int>)> modelTask, LanguageModel? languageModel)
    {
        return new(() =>
        {
            modelTask.Wait();

            (var probs, var sizes) = modelTask.Result;

            return _model.Decode(probs, sizes, languageModel);
        });
    }

    private Task<List<string>> DecodeTask(Task<(Tensor<float>, Tensor<int>)> modelTask, int batchSize, int batchIndex, LanguageModel? languageModel)
    {
        return new(() =>
        {
            modelTask.Wait();

            (var allProbs, var allSizes) = modelTask.Result;
            var dim2 = allProbs.Dimensions[2];
            var sDim = allSizes.Dimensions[1];

            // Get max sequence length
            var dim1 = -1;
            for (var i = 0; i < batchSize; i++)
            {
                var len = allSizes[batchIndex + i, 0];
                if (len > dim1)
                {
                    dim1 = (int)len;
                }
            }

            // Extract only the data for this batch
            var probs = new DenseTensor<float>(new int[] { batchSize, dim1, dim2 });
            var sizes = new DenseTensor<int>(new int[] { batchSize, sDim });
            for (var i = 0; i < batchSize; i++)
            {
                for (var j = 0; j < dim1; j++)
                {
                    for (var k = 0; k < dim2; k++)
                    {
                        probs[i, j, k] = allProbs[batchIndex + i, j, k];
                    }
                }

                for (var j = 0; j < sDim; j++)
                {
                    sizes[i, j] = allSizes[batchIndex + i, j];
                }
            }

            return _model.Decode(probs, sizes, languageModel);
        });
    }
}