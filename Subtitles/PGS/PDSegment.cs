using System.Collections.ObjectModel;

namespace subtitle_ocr_console.Subtitles.PGS;

class PDSegment : Segment
{
    public byte PaletteID { get; private set; }
    public byte VersionNumber { get; private set; }
    public List<PaletteEntry> _entries = new();
    public ReadOnlyCollection<PaletteEntry> Entries => _entries.AsReadOnly();

    public PDSegment(SegmentHeader header, BinaryReader reader)
        : base(header)
    {
        InitializeFromBinary(reader);
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        try
        {
            PaletteID = reader.ReadByte();
            VersionNumber = reader.ReadByte();

            InitializeEntries(reader);
        }
        catch (Exception ex) when (ex is not PGSReadException)
        {
            throw new PGSReadException("Internal exception when reading PD segment", ex);
        }
    }

    private void InitializeEntries(BinaryReader reader)
    {
        int bytesRead = 2;
        while ((Header.Size - bytesRead) >= 5)
        {
            var paletteEntry = new PaletteEntry(reader);
            bytesRead += 5;

            _entries.Add(paletteEntry);
        }

        if (bytesRead < Header.Size)
        {
            throw new PGSReadException("PD segment has data left over after reading palette entries");
        }
    }
}