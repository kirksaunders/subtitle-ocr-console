namespace subtitle_ocr_console.Subtitles.PGS;

public static class RunLengthDecoder
{
    public static IEnumerable<(byte, int)> Decode(byte[] encoding)
    {
        var seekPos = 0;
        while (seekPos < encoding.Length)
        {
            var first = encoding[seekPos];
            seekPos += 1;

            if (first == 0)
            {
                var second = encoding[seekPos];
                seekPos += 1;

                if (second != 0)
                {
                    var mode = 0xC0 & second;

                    if (mode == 0x00)
                    {
                        yield return (0, second);
                    }
                    else if (mode == 0x40)
                    {
                        var third = encoding[seekPos];
                        seekPos += 1;

                        var numPixels = ((int)(second & 0x3F) << 8) + third;

                        yield return (0, numPixels);
                    }
                    else if (mode == 0x80)
                    {
                        var third = encoding[seekPos];
                        seekPos += 1;

                        var numPixels = second & 0x3F;

                        yield return (third, numPixels);
                    }
                    else if (mode == 0xC0)
                    {
                        var third = encoding[seekPos];
                        var fourth = encoding[seekPos + 1];
                        seekPos += 2;

                        var numPixels = ((int)(second & 0x3F) << 8) + third;

                        yield return (fourth, numPixels);
                    }
                }
                else
                {
                    // TODO: End of line?
                }
            }
            else
            {
                yield return (first, 1);
            }
        }
    }
}
