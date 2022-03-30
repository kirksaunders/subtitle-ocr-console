using subtitle_ocr_console.Subtitles.PGS;

using (var stream = File.Open("/home/user/Downloads/subs.sup", FileMode.Open))
{
    using (var reader = new EndiannessAwareBinaryReader(stream, System.Text.Encoding.UTF8, false, EndiannessAwareBinaryReader.Endianness.Big))
    {
        try
        {
            var pgs = PGSReader.ReadFromBinary(reader);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.ToString());
        }
    }
}
