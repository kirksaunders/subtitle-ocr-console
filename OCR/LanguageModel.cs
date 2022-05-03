using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace subtitle_ocr_console.OCR;

public class LanguageModel
{
    // Probabilities of a character occurring as the first character of a word.
    private double[] _firstCharProbs;
    private double[] _firstCharCumulativeProbs;

    // Probabilities of a character occurring after another character.
    // Ex: _secondCharProbs[c1][c2] = P(c2 | c1)
    private double[][] _secondCharProbs;
    private double[][] _secondCharCumulativeProbs;

    private readonly Codec _codec;

    public LanguageModel(Codec codec, IEnumerable<FileInfo> lineDataPaths)
    {
        _codec = codec;

        // Allocate memory for probabilities
        _firstCharProbs = new double[_codec.Count];
        _firstCharCumulativeProbs = new double[_codec.Count];
        _secondCharProbs = new double[_codec.Count][];
        _secondCharCumulativeProbs = new double[_codec.Count][];
        for (var i = 0; i < _codec.Count; i++)
        {
            _secondCharProbs[i] = new double[_codec.Count];
            _secondCharCumulativeProbs[i] = new double[_codec.Count];
        }

        CalculateProbs(lineDataPaths);
        CalculateCululativeProbs();
    }

    public LanguageModel(Codec codec, FileInfo savePath)
    {
        _codec = codec;

        string jsonString = File.ReadAllText(savePath.FullName);
        LoadFromJson(jsonString);
    }

    public LanguageModel(Codec codec, Stream inputStream)
    {
        _codec = codec;

        var reader = new StreamReader(inputStream);
        string jsonString = reader.ReadToEnd();
        LoadFromJson(jsonString);
    }

    // These annotations are here to suppress a warning by saying "this function defines these fields"
    [MemberNotNull(nameof(_firstCharProbs))]
    [MemberNotNull(nameof(_firstCharCumulativeProbs))]
    [MemberNotNull(nameof(_secondCharProbs))]
    [MemberNotNull(nameof(_secondCharCumulativeProbs))]
    private void LoadFromJson(string jsonString)
    {
        JsonSerializerOptions options = new() { IncludeFields = true };
        (var firstCharProbs, var secondCharProbs) = JsonSerializer.Deserialize<(double[], double[][])>(jsonString, options);

        if (firstCharProbs == null || secondCharProbs == null)
        {
            throw new ArgumentException("Unable to read language model from file");
        }

        // Ensure size matches
        if (firstCharProbs.Length != _codec.Count || secondCharProbs.Length != _codec.Count)
        {
            throw new ArgumentException("Loaded language model size doesn't match codec size");
        }
        foreach (var r in secondCharProbs)
        {
            if (r.Length != _codec.Count)
            {
                throw new ArgumentException("Loaded language model size doesn't match codec size");
            }
        }

        _firstCharProbs = firstCharProbs;
        _secondCharProbs = secondCharProbs;

        // Allocate memory for cumulative probabilities
        _firstCharCumulativeProbs = new double[_codec.Count];
        _secondCharCumulativeProbs = new double[_codec.Count][];
        for (var i = 0; i < _codec.Count; i++)
        {
            _secondCharCumulativeProbs[i] = new double[_codec.Count];
        }

        CalculateCululativeProbs();
    }

    private void CalculateProbs(IEnumerable<FileInfo> lineDataPaths)
    {
        int[] firstCharCounts = new int[_codec.Count];
        int numFirstChars = 0;
        int[,] secondCharCounts = new int[_codec.Count, _codec.Count];
        int[] numSecondChars = new int[_codec.Count];

        // Count number of occurrences of characters
        foreach (var lineDataPath in lineDataPaths)
        {
            var lines = LineDataReader.ReadLines(_codec, lineDataPath);
            foreach (var line in lines)
            {
                var chars = line.ToCharArray();
                int lastIndex = -1;
                for (var i = 0; i < chars.Length; i++)
                {
                    int index = _codec.GetCharacterIndex(chars[i]);

                    if (index < 0)
                    {
                        throw new InvalidOperationException("This code should be unreachable");
                    }

                    if (i == 0)
                    {
                        firstCharCounts[index]++;
                        numFirstChars++;
                    }
                    else
                    {
                        secondCharCounts[lastIndex, index]++;
                        numSecondChars[lastIndex]++;
                    }

                    lastIndex = index;
                }
            }
        }

        // Calculate probabilities from counts
        for (var i = 0; i < _codec.Count; i++)
        {
            _firstCharProbs[i] = (double)firstCharCounts[i] / numFirstChars;

            for (var j = 0; j < _codec.Count; j++)
            {
                int denom = numSecondChars[i];
                if (denom > 0)
                {
                    _secondCharProbs[i][j] = (double)secondCharCounts[i, j] / denom;
                }
            }
        }
    }

    private void CalculateCululativeProbs()
    {
        _firstCharCumulativeProbs[0] = _firstCharProbs[0];
        for (var i = 1; i < _firstCharProbs.Length; i++)
        {
            _firstCharCumulativeProbs[i] = _firstCharCumulativeProbs[i - 1] + _firstCharProbs[i];
        }

        for (var i = 0; i < _secondCharProbs.Length; i++)
        {
            _secondCharCumulativeProbs[i][0] = _secondCharProbs[i][0];
            for (var j = 1; j < _secondCharProbs.Length; j++)
            {
                _secondCharCumulativeProbs[i][j] = _secondCharCumulativeProbs[i][j - 1] + _secondCharProbs[i][j];
            }
        }
    }

    public double GetProbability(int charIndex)
    {
        if (charIndex < 0 || charIndex >= _codec.Count)
        {
            throw new ArgumentOutOfRangeException("Index out of range for codec");
        }

        return _firstCharProbs[charIndex];
    }

    public double GetProbability(int firstCharIndex, int secondCharIndex)
    {
        if (firstCharIndex < 0 || firstCharIndex >= _codec.Count)
        {
            throw new ArgumentOutOfRangeException("Index out of range for codec");
        }
        if (secondCharIndex < 0 || secondCharIndex >= _codec.Count)
        {
            throw new ArgumentOutOfRangeException("Index out of range for codec");
        }

        return _secondCharProbs[firstCharIndex][secondCharIndex];
    }

    private static readonly Random randomGenerator = new();

    private static int SampleDistribution(double[] distribution)
    {
        double p = Math.Max(0.000001, randomGenerator.NextDouble() * distribution[^1]);
        int index = Array.BinarySearch(distribution, p);

        // If the exact item isn't found, the docs say that "the negative number returned is
        // the bitwise complement of the index of the first element that is larger than value.
        // If value is not found and value is greater than all elements in array, the negative
        // number returned is the bitwise complement of (the index of the last element plus 1)."
        if (index < 0)
        {
            index = ~index;
        }
        if (index >= distribution.Length)
        {
            index = distribution.Length - 1;
        }

        // Handle zero probabilities (if distribution[index] == distribution[index-1], then
        // that means the item at index has zero probability and must not be selected).
        // TODO: Determine whether this step is necessary. BinarySearch may already handle
        //       these cases properly.
        while (index > 0 && distribution[index] == distribution[index - 1])
        {
            index--;
        }

        return index;
    }

    public CodecCharacter SampleCharacter()
    {
        int index = SampleDistribution(_firstCharCumulativeProbs);
        var character = _codec.GetCharacter(index) ?? throw new InvalidOperationException("This code should be unreachable");

        return character;
    }

    public CodecCharacter SampleCharacter(char previous)
    {
        int prevIndex = _codec.GetCharacterIndex(previous);
        int index = SampleDistribution(_secondCharCumulativeProbs[prevIndex]);
        var character = _codec.GetCharacter(index) ?? throw new InvalidOperationException("This code should be unreachable");

        return character;
    }

    public CodecCharacter SampleCharacterUniform()
    {
        int index = randomGenerator.Next(_codec.Count);
        var character = _codec.GetCharacter(index) ?? throw new InvalidOperationException("This code should be unreachable");

        return character;
    }

    public void Save(FileInfo path)
    {
        JsonSerializerOptions options = new() { IncludeFields = true };

        var data = (_firstCharProbs, _secondCharProbs);

        string jsonString = JsonSerializer.Serialize(data, options);
        File.WriteAllText(path.FullName, jsonString);
    }
}