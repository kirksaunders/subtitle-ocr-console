using static subtitle_ocr_console.Utils.Logarithms;

namespace subtitle_ocr_console.OCR.Decoders;

public class BeamEntry : IComparable<BeamEntry>
{
    public struct Probability
    {
        public float Total = LOG_0;
        public float Blank = LOG_0;
        public float Label = LOG_0;

        public Probability()
        {
        }

        public void Reset()
        {
            Total = LOG_0;
            Blank = LOG_0;
            Label = LOG_0;
        }
    }

    public BeamEntry? Parent { get; private set; }
    public int Label { get; private set; }
    private Dictionary<int, BeamEntry> _children = new();

    public Probability OldP = new();
    public Probability NewP = new();

    public BeamEntry(BeamEntry? parent, int label)
    {
        Parent = parent;
        Label = label;
    }

    public int CompareTo(BeamEntry? other)
    {
        if (other == null)
        {
            return 1;
        }

        return NewP.Total.CompareTo(other.NewP.Total);
    }

    public static bool operator <(BeamEntry a, BeamEntry b)
    {
        return a.NewP.Total < b.NewP.Total;
    }

    public static bool operator >(BeamEntry a, BeamEntry b)
    {
        return a.NewP.Total > b.NewP.Total;
    }

    public bool Active()
    {
        return NewP.Total != LOG_0;
    }

    public BeamEntry GetChild(int label)
    {
        BeamEntry? child;
        if (!_children.TryGetValue(label, out child))
        {
            child = new BeamEntry(this, label);
            _children[label] = child;
        }

        return child;
    }

    public List<int> LabelSequence()
    {
        List<int> seq = new();

        // Note: This skips the root entry. That is desired
        BeamEntry entry = this;
        while (entry.Parent != null)
        {
            seq.Insert(0, entry.Label);
            entry = entry.Parent;
        }

        return seq;
    }
}