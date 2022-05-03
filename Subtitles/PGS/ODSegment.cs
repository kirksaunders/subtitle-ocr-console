using System.Diagnostics.CodeAnalysis;

namespace subtitle_ocr_console.Subtitles.PGS;

class ODSegment : Segment
{
    public enum LastInSequence
    {
        Last,
        First,
        Both
    }

    public ushort ObjectID { get; private set; }
    public byte VersionNumber { get; private set; }
    public LastInSequence Last { get; private set; }
    public ushort Width { get; private set; }
    public ushort Height { get; private set; }
    public uint DataLength { get; private set; } // One byte wasted (spec has this as 3 bytes)
    public byte[] RLEData;

    public ODSegment(SegmentHeader header, BinaryReader reader)
        : base(header)
    {
        InitializeFromBinary(reader);
    }

    // This annotation are here to suppress a warning by saying "this function defines this field"
    [MemberNotNull(nameof(RLEData))]
    private void InitializeFromBinary(BinaryReader reader)
    {
        try
        {
            ObjectID = reader.ReadUInt16();
            VersionNumber = reader.ReadByte();

            byte flag = reader.ReadByte();

            Last = flag switch
            {
                0x40 => LastInSequence.Last,
                0x80 => LastInSequence.First,
                0xC0 => LastInSequence.Both,
                _ => throw new PGSReadException($"Unknown last in sequence flag: {flag}"),
            };

            int bytesRead = 4;
            if (Last == LastInSequence.First || Last == LastInSequence.Both)
            {
                var bytes = reader.ReadBytes(3);
                DataLength = ((uint)bytes[0] << 16) | ((uint)bytes[1] << 8) | bytes[2];

                Width = reader.ReadUInt16();
                Height = reader.ReadUInt16();

                bytesRead += 7;
            }

            var numBytes = Header.Size - bytesRead;
            RLEData = reader.ReadBytes(numBytes);
            bytesRead += numBytes;
        }
        catch (Exception ex) when (ex is not PGSReadException)
        {
            throw new PGSReadException("Internal exception when reading OD segment", ex);
        }
    }
}