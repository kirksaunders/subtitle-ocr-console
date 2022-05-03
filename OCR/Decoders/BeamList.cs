namespace subtitle_ocr_console.OCR.Decoders;

public class BeamList
{
    private BeamEntry[] _data;
    private BeamEntry[] _dataSwap;
    private readonly int _capacity;
    private int _size = 0;

    public int Count { get { return _size; } }

    public BeamList(int capacity)
    {
        _data = new BeamEntry[capacity];
        _dataSwap = new BeamEntry[capacity];
        _capacity = capacity;
    }

    public BeamEntry? Add(BeamEntry item)
    {
        if (_size < _capacity)
        {
            int idx = _size;
            _data[idx] = item;
            _size++;

            SiftUp(idx);

            return null;
        }
        else
        {
            BeamEntry removed = _data[0];
            _data[0] = item;
            SiftDown(0);

            return removed;
        }
    }

    public ReadOnlySpan<BeamEntry> AsSpan()
    {
        return _data.AsSpan()[.._size];
    }

    public void Clear()
    {
        Array.Clear(_data, 0, _capacity);
        Array.Clear(_dataSwap, 0, _capacity);
        _size = 0;
    }

    public void SwapAndClear()
    {
        (_data, _dataSwap) = (_dataSwap, _data);
        Array.Clear(_data, 0, _size);
        _size = 0;
    }

    public BeamEntry GetMinimum()
    {
        if (_size == 0)
        {
            throw new IndexOutOfRangeException("Index out of range");
        }

        return _data[0];
    }

    private void SiftUp(int idx)
    {
        while (idx > 0)
        {
            int parent = (idx - 1) / 2;

            if (_data[idx].CompareTo(_data[parent]) < 0)
            {
                (_data[idx], _data[parent]) = (_data[parent], _data[idx]);
            }
            else
            {
                break;
            }

            idx = parent;
        }
    }

    private void SiftDown(int idx)
    {
        while (idx < _size)
        {
            int left = 2 * idx + 1;
            int right = left + 1;

            // Get child with smaller value
            int child;
            if (left < _size && right < _size)
            {
                if (_data[left].CompareTo(_data[right]) < 0)
                {
                    child = left;
                }
                else
                {
                    child = right;
                }
            }
            else if (left < _size)
            {
                child = left;
            }
            else
            {
                break;
            }

            if (_data[idx].CompareTo(_data[child]) > 0)
            {
                (_data[idx], _data[child]) = (_data[child], _data[idx]);
            }
            else
            {
                break;
            }

            idx = child;
        }
    }

    public List<BeamEntry> ToList()
    {
        if (_size == 0)
        {
            return new List<BeamEntry>();
        }

        return new List<BeamEntry>(_data[0.._size]);
    }
}