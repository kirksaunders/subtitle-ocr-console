namespace subtitle_ocr_console.Subtitles.PGS;

public class PGSStateException : Exception
{
    public PGSStateException()
    {
    }

    public PGSStateException(string message)
        : base(message)
    {
    }

    public PGSStateException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
