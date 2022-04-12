using System.Text;

namespace subtitle_ocr_console.OCR;

public static class LineDataReader
{
    public static IEnumerable<string> ReadLines(Codec codec, string path)
    {
        var lines = File.ReadLines(path);
        foreach (var line in lines)
        {
            var str = new StringBuilder(line.Length);
            int spaceCount = 0;
            foreach (char c in line.ToCharArray())
            {
                if (c == ' ')
                {
                    spaceCount++;
                }
                else
                {
                    if (codec.GetCharacterIndex(c) >= 0)
                    {
                        if (spaceCount > 0)
                        {
                            str.Append(' ', spaceCount);
                            spaceCount = 0;
                        }
                        str.Append(c);
                    }
                    else
                    {
                        Console.Error.WriteLine($"Warning: Character '{c}' from line data file is not in codec");
                    }
                }
            }

            if (str.Length > 0)
            {
                yield return str.ToString();
            }
        }
    }
}