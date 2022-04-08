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
    public char Char;
    public CodecCharacterType Type;

    public int CompareTo(CodecCharacter other)
    {
        return Char.CompareTo(other.Char);
    }
}