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
    public List<byte> Pixels { get; } = new();

    public ODSegment(SegmentHeader header, BinaryReader reader)
        : base(header)
    {
        InitializeFromBinary(reader);
    }

    private void InitializeFromBinary(BinaryReader reader)
    {
        try
        {
            ObjectID = reader.ReadUInt16();
            VersionNumber = reader.ReadByte();

            byte flag = reader.ReadByte();

            switch (flag)
            {
                case 0x40:
                    Last = LastInSequence.Last;
                    break;

                case 0x80:
                    Last = LastInSequence.First;
                    break;

                case 0xC0:
                    Last = LastInSequence.Both;
                    break;

                default:
                    throw new PGSReadException($"Unknown last in sequence flag: {flag}");
            }

            int bytesRead = 4;
            if (Last == LastInSequence.First || Last == LastInSequence.Both)
            {
                var bytes = reader.ReadBytes(3);
                DataLength = ((uint)bytes[0] << 16) | ((uint)bytes[1] << 8) | bytes[2];

                Width = reader.ReadUInt16();
                Height = reader.ReadUInt16();

                bytesRead += 7;
            }

            InitializePixels(reader, bytesRead);
        }
        catch (Exception ex) when (ex is not PGSReadException)
        {
            throw new PGSReadException("Internal exception when reading OD segment", ex);
        }
    }

    private void InitializePixels(BinaryReader reader, int bytesRead)
    {
        while ((Header.Size - bytesRead) > 0)
        {
            var first = reader.ReadByte();
            bytesRead += 1;

            if (first == 0)
            {
                var second = reader.ReadByte();
                bytesRead += 1;

                if (second != 0)
                {
                    var mode = 0xC0 & second;

                    if (mode == 0x00)
                    {
                        for (int i = 0; i < second; i++)
                        {
                            Pixels.Add(0);
                        }
                    }
                    else if (mode == 0x40)
                    {
                        var third = reader.ReadByte();
                        bytesRead += 1;

                        var numPixels = ((uint)(second & 0x3F) << 8) | third;

                        for (int i = 0; i < numPixels; i++)
                        {
                            Pixels.Add(0);
                        }
                    }
                    else if (mode == 0x80)
                    {
                        var third = reader.ReadByte();
                        bytesRead += 1;

                        var numPixels = second & 0x3F;

                        for (int i = 0; i < numPixels; i++)
                        {
                            Pixels.Add(third);
                        }
                    }
                    else if (mode == 0xC0)
                    {
                        var third = reader.ReadByte();
                        var fourth = reader.ReadByte();
                        bytesRead += 2;

                        var numPixels = ((uint)(second & 0x3F) << 8) | third;

                        for (int i = 0; i < numPixels; i++)
                        {
                            Pixels.Add(fourth);
                        }
                    }
                }
            }
            else
            {
                Pixels.Add(first);
            }
        }
    }
}