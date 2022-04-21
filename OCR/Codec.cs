using System.Text.Json;

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

    public Codec(FileInfo savePath)
    {
        string jsonString = File.ReadAllText(savePath.FullName);
        var chars = JsonSerializer.Deserialize<List<CodecCharacter>>(jsonString);

        if (chars == null)
        {
            throw new ArgumentException("Unable to read codec from file");
        }

        _characters = chars;
    }

    /// <summary>
    /// Method <c>GetCharacterIndex</c>
    /// <returns>
    /// Returns the index within the codec of the given character, or a negative
    /// value if not found.
    /// </returns>
    /// </summary>
    public int GetCharacterIndex(char c)
    {
        int index = _characters.BinarySearch(new CodecCharacter() { Char = c });
        if (index >= 0)
        {
            return index;
        }

        return -1;
    }

    /// <summary>
    /// Method <c>GetCharacter</c>
    /// <returns>
    /// Returns the character at the given index within the codec, or
    /// null if not found.
    /// </returns>
    /// </summary>
    public CodecCharacter? GetCharacter(int index)
    {
        if (index < 0 || index >= _characters.Count)
        {
            return null;
        }

        return _characters[index];
    }

    public void Save(FileInfo path)
    {
        string jsonString = JsonSerializer.Serialize(_characters);
        File.WriteAllText(path.FullName, jsonString);
    }
}