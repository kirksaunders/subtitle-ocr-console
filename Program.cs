using subtitle_ocr_console.Subtitles.PGS;
using subtitle_ocr_console.Utils;

using (var stream = File.Open("/home/user/Downloads/subs.sup", FileMode.Open))
{
    using (var reader = new EndiannessAwareBinaryReader(stream, System.Text.Encoding.UTF8, false, EndiannessAwareBinaryReader.Endianness.Big))
    {
        try
        {
            var pgs = new PGSReader(reader);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.ToString());
        }
    }
}
