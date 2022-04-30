namespace subtitle_ocr_console.Subtitles.SRT;

public class SRTReadException : Exception
{
    public SRTReadException()
    {
    }

    public SRTReadException(string message)
        : base(message)
    {
    }

    public SRTReadException(string message, Exception inner)
        : base(message, inner)
    {
    }
}