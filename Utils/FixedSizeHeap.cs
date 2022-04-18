namespace subtitle_ocr_console.OCR.Decoders;

public class FixedSizeHeap<T> where T : IComparable<T>
{
    private T[] _data;
    private int _capacity;
    private int _size = 0;

    public int Count { get { return _size; } }

    public FixedSizeHeap(int capacity)
    {
        _data = new T[capacity];
        _capacity = capacity;
    }

    public bool Add(T item)
    {
        if (_size < _capacity)
        {
            int idx = _size;
            _data[idx] = item;
            _size++;

            SiftUp(idx);
        }
        else
        {
            if (item.CompareTo(_data[0]) < 0)
            {
                return false;
            }

            _data[0] = item;
            SiftDown(0);
        }

        return true;
    }

    public void Clear()
    {
        _size = 0;
    }

    public T GetMinimum()
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
                T tmp = _data[parent];
                _data[parent] = _data[idx];
                _data[idx] = tmp;
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
            int right = 2 * idx + 2;

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
                T tmp = _data[child];
                _data[child] = _data[idx];
                _data[idx] = tmp;
            }
            else
            {
                break;
            }

            idx = child;
        }
    }

    public List<T> ToList()
    {
        if (_size == 0)
        {
            return new List<T>();
        }

        return new List<T>(_data[0.._size]);
    }
}