using System.Collections.ObjectModel;

namespace subtitle_ocr_console.Subtitles.SRT;

public class SRT
{
    private List<SRTFrame> _frames = new();
    public ReadOnlyCollection<SRTFrame> Frames { get { return _frames.AsReadOnly(); } }

    private static IComparer<SRTFrame> _frameComparer = Comparer<SRTFrame>.Create((x, y) => x.StartTimestamp.CompareTo(y.StartTimestamp));


    public SRT()
    {
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