using System.Collections.ObjectModel;
using System.Globalization;

namespace subtitle_ocr_console.Subtitles.SRT;

public class SRT
{
    private List<SRTFrame> _frames = new();
    public ReadOnlyCollection<SRTFrame> Frames { get { return _frames.AsReadOnly(); } }

    private static IComparer<SRTFrame> _frameComparer = Comparer<SRTFrame>.Create((x, y) => x.StartTimestamp.CompareTo(y.StartTimestamp));

    public SRT()
    {
    }

    public SRT(FileInfo savePath)
    {
        var lines = File.ReadLines(savePath.FullName);
        SRTFrame? frame = null;
        int lineNum = 0;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            lineNum++;

            if (frame == null)
            {
                if (trimmed.Length == 0)
                {
                    // If line is empty, just skip
                    continue;
                }

                // If frame is null, we expect to see the subtitle id
                int id;
                bool success = int.TryParse(trimmed, out id);
                if (!success)
                {
                    throw new SRTReadException($"Expected subtitle id at line {lineNum}, but unable to parse as int");
                }

                if (id <= 0)
                {
                    throw new SRTReadException($"Unexpected subtitle id at line {lineNum}, ids should be positive");
                }

                frame = new();
                frame.StartTimestamp = -1;
                frame.EndTimestamp = -1;
            }
            else
            {
                // If frame start and end timestamp are -1, we have only read id so far
                if (frame.StartTimestamp == -1 && frame.EndTimestamp == -1)
                {
                    // We expect to read the start and end timestamps separated by " --> "
                    string[] strs = trimmed.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                    if (strs.Length != 3 || !strs[1].Equals("-->"))
                    {
                        throw new SRTReadException($"Incorrect timestamp format at line {lineNum}");
                    }

                    var start = ParseTimestamp(strs[0]) ?? throw new SRTReadException($"Incorrect start timestamp format at line {lineNum}");
                    frame.StartTimestamp = (int)(start.Ticks / TimeSpan.TicksPerMillisecond);

                    var end = ParseTimestamp(strs[0]) ?? throw new SRTReadException($"Incorrect end timestamp format at line {lineNum}");
                    frame.EndTimestamp = (int)(end.Ticks / TimeSpan.TicksPerMillisecond);
                }
                else
                {
                    // We expect either an empty line to signify end of text or some text
                    if (trimmed.Length == 0)
                    {
                        // Frame is commplete, move on to next
                        AddFrame(frame);
                        frame = null;
                    }
                    else
                    {
                        // Add text to frame as new line
                        frame.Text = frame.Text + (frame.Text.Length > 0 ? '\n' : "") + line;
                    }
                }
            }
        }
    }

    private static CultureInfo _culture = new("hr-HR");

    private TimeSpan? ParseTimestamp(string input)
    {
        TimeSpan ts;
        bool success = TimeSpan.TryParse(input, _culture, out ts);

        return success ? ts : null;
    }

    public void AddFrame(SRTFrame frame)
    {
        // Insert sorted by YPos
        int pos = _frames.BinarySearch(frame, _frameComparer);
        if (pos < 0)
        {
            pos = ~pos;
        }
        _frames.Insert(pos, frame);
    }

    public void Write(FileInfo path)
    {
        using (var writer = new StreamWriter(path.FullName))
        {
            // For comma separator for milliseconds instead of period
            for (var i = 0; i < _frames.Count; i++)
            {
                var frame = _frames[i];
                var startTime = new TimeSpan(0, 0, 0, 0, frame.StartTimestamp);
                var endTime = new TimeSpan(0, 0, 0, 0, frame.EndTimestamp);

                var t1 = startTime.ToString(@"hh\:mm\:ss\,fff");
                var t2 = endTime.ToString(@"hh\:mm\:ss\,fff");

                writer.WriteLine($"{i + 1}");
                writer.WriteLine($"{t1} --> {t2}");
                writer.WriteLine(frame.Text);
                writer.WriteLine("");
            }
        }
    }
}