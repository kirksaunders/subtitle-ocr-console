using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using Tesseract;

namespace subtitle_ocr_console.OCR;

public class TesseractModel
{
    private TesseractEngine _engine;

    public TesseractModel(DirectoryInfo path, string language)
    {
        _engine = new TesseractEngine(path.FullName, language, EngineMode.Default);
    }

    private static Pix ToPix<TPixel>(Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {
        using (var memoryStream = new MemoryStream())
        {
            var imageEncoder = image.GetConfiguration().ImageFormatsManager.FindEncoder(PngFormat.Instance);
            image.Save(memoryStream, imageEncoder);

            memoryStream.Seek(0, SeekOrigin.Begin);

            return Pix.LoadFromMemory(memoryStream.ToArray());
        }
    }

    public string Infer<TPixel>(Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {
        using (var img = ToPix(image))
        {
            using (var page = _engine.Process(img))
            {
                return page.GetText().Trim();
            }
        }
    }
}