namespace subtitle_ocr_console.OCR;

public enum CodecCharacterType
{
    Letter,
    Digit,
    Whitespace,
    Punctuation
}

public struct CodecCharacter : IComparable<CodecCharacter>
{
    public char Char { get; set; }
    public CodecCharacterType Type { get; set; }

    public int CompareTo(CodecCharacter other)
    {
        return Char.CompareTo(other.Char);
    }
}