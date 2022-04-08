namespace subtitle_ocr_console.OCR;

public class Codec
{
    private List<CodecCharacter> _characters = new();

    public int Count { get { return _characters.Count; } }

    public Codec(params (char startChar, char endChar, CodecCharacterType type)[] characters)
    {
        foreach ((var startChar, var endChar, var type) in characters)
        {
            for (int i = startChar; i <= endChar; i++)
            {
                _characters.Add(new CodecCharacter() { Char = (char)i, Type = type });
            }
        }

        // Sort characters by their character value
        _characters.Sort();
    }

    public int GetCharacterIndex(char c)
    {
        int index = _characters.BinarySearch(new CodecCharacter() { Char = c });
        if (index >= 0)
        {
            return index;
        }

        throw new ArgumentException("Character not found in codec");
    }

    public CodecCharacter GetCharacter(int index)
    {
        if (index < 0 || index >= _characters.Count)
        {
            throw new ArgumentException("Index out of range");
        }

        return _characters[index];
    }
}