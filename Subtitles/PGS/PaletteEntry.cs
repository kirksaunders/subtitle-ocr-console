using System.IO;
using SixLabors.ImageSharp.PixelFormats;

namespace subtitle_ocr_console.Subtitles.PGS;

class PaletteEntry
{
    public byte EntryID { get; private set; }
    public byte Y { get; private set; }
    public byte Cr { get; private set; }
    public byte Cb { get; private set; }
    public byte Alpha { get; private set; }

    public PaletteEntry()
    {
    }

    public static PaletteEntry ReadFromBinary(BinaryReader reader)
    {
        var instance = new PaletteEntry();
        instance.InitializeFromBinary(reader);

        return instance;
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        try
        {
            EntryID = reader.ReadByte();
            Y = reader.ReadByte();
            Cr = reader.ReadByte();
            Cb = reader.ReadByte();
            Alpha = reader.ReadByte();
        }
        catch (Exception ex) when (ex is not PGSReadException)
        {
            throw new PGSReadException("Internal exception when reading palette entry", ex);
        }
    }

    // Source: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-rdprfx/2e1618ed-60d6-4a64-aa5d-0608884861bb
    public Rgba32 AsRGBA()
    {
        var cr = (double)Cr - 128;
        var cb = (double)Cb - 128;

        var r = 1.0 * Y + 1.402525 * cr;
        var g = 1.0 * Y - 0.343730 * cb - 0.714401 * cr;
        var b = 1.0 * Y + 1.769905 * cb + 0.000013 * cr;

        byte rByte = Convert.ToByte(Math.Clamp(r, 0, 255));
        byte gByte = Convert.ToByte(Math.Clamp(g, 0, 255));
        byte bByte = Convert.ToByte(Math.Clamp(b, 0, 255));

        return new Rgba32(rByte, gByte, bByte, Alpha);
    }
}