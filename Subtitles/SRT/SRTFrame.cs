namespace subtitle_ocr_console.Subtitles.SRT;

public class SRTFrame
{
    // Timestamps are in milliseconds
    public int StartTimestamp;
    public int EndTimestamp;
    public string Text = "";

    public SRTFrame()
    {
    }

    public SRTFrame(string text)
    {
        Text = text;
    }

    public SRTFrame(int startTimestamp, string text)
    {
        StartTimestamp = startTimestamp;
        Text = text;
    }

    public SRTFrame(int startTimestamp, int endTimestamp, string text)
    {
        StartTimestamp = startTimestamp;
        EndTimestamp = endTimestamp;
        Text = text;
    }
}