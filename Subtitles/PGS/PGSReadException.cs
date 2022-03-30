namespace subtitle_ocr_console.Subtitles.PGS;

public class PGSReadException : Exception
{
    public PGSReadException()
    {
    }

    public PGSReadException(string message)
        : base(message)
    {
    }

    public PGSReadException(string message, Exception inner)
        : base(message, inner)
    {
    }
}