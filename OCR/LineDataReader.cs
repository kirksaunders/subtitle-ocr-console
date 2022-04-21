using System.Text;

namespace subtitle_ocr_console.OCR;

public static class LineDataReader
{
    private static IEnumerable<char> ReadChars(Codec codec, string line)
    {
        foreach (char c in line.ToCharArray())
        {
            if (c == '\u201C' || c == '\u201D')
            {
                yield return '"';
            }
            else if (c == '\u2019')
            {
                yield return '\'';
            }
            else if (c == '\u2014')
            {
                yield return '-';
            }
            else
            {
                yield return c;
            }
        }
    }

    public static IEnumerable<string> ReadLines(Codec codec, FileInfo path)
    {
        var lines = File.ReadLines(path.FullName);
        foreach (var line in lines)
        {
            var str = new StringBuilder(line.Length);
            int spaceCount = 0;
            foreach (char c in ReadChars(codec, line))
            {
                if (Char.IsWhiteSpace(c))
                {
                    if (str.Length > 0)
                    {
                        spaceCount++;
                    }
                }
                else
                {
                    if (codec.GetCharacterIndex(c) >= 0)
                    {
                        if (spaceCount > 0)
                        {
                            // Currently condensing multiple spaces into one
                            str.Append(' ');
                            spaceCount = 0;
                        }
                        str.Append(c);
                    }
                    else
                    {
                        //Console.Error.WriteLine($"Warning: Character '{c}' from line data file is not in codec");
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