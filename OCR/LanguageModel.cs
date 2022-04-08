namespace subtitle_ocr_console.OCR;

public class LanguageModel
{
    // Probabilities of a character occurring as the first character of a word.
    private List<double> _firstCharProbs = new();
    private List<double> _firstCharCumulativeProbs = new();

    // Probabilities of a character occurring after another character.
    // Ex: _secondCharProbs[c1][c2] = P(c2 | c1)
    private List<List<double>> _secondCharProbs = new();
    private List<List<double>> _secondCharCumulativeProbs = new();

    private Codec _codec;

    public LanguageModel(Codec codec, string dictionaryPath)
    {
        _codec = codec;

        CalculateProbs(dictionaryPath);
        CalculateCululativeProbs();
    }

    private void CalculateProbs(string dictionaryPath)
    {
        List<int> firstCharCounts = new(new int[_codec.Count]);
        int numFirstChars = 0;
        List<List<int>> secondCharCounts = new(_codec.Count);
        for (var i = 0; i < _codec.Count; i++)
        {
            secondCharCounts.Add(new(new int[_codec.Count]));
        }
        List<int> numSecondChars = new(new int[_codec.Count]);
        List<int> finalCharCounts = new(new int[_codec.Count]);
        int numFinalChars = 0;

        // Count number of occurrences of characters
        var lines = File.ReadLines(dictionaryPath);
        foreach (var line in lines)
        {
            var chars = line.ToCharArray();
            int lastIndex = -1;
            for (var i = 0; i < chars.Length; i++)
            {
                int index = _codec.GetCharacterIndex(chars[i]);

                if (i == 0)
                {
                    firstCharCounts[index]++;
                    numFirstChars++;
                }
                else
                {
                    secondCharCounts[lastIndex][index]++;
                    numSecondChars[lastIndex]++;
                }

                if (i == chars.Length - 1)
                {
                    finalCharCounts[index]++;
                    numFinalChars++;
                }

                lastIndex = index;
            }
        }

        // Reserve space for probabilities
        _firstCharProbs = new(new double[_codec.Count]);
        _secondCharProbs = new(_codec.Count);

        // Calculate probabilities from counts
        for (var i = 0; i < _codec.Count; i++)
        {
            var type1 = _codec.GetCharacter(i).Type;

            _firstCharProbs[i] = (double)firstCharCounts[i] / numFirstChars;

            _secondCharProbs.Add(new(new double[_codec.Count]));
            for (var j = 0; j < _codec.Count; j++)
            {
                var type2 = _codec.GetCharacter(j).Type;
                if (type1 == CodecCharacterType.Whitespace || type1 == CodecCharacterType.Punctuation)
                {
                    // Treat first char probabilities as second char when first char is whitespace or punctuation
                    int denom = numSecondChars[i] + numFirstChars;
                    if (denom > 0)
                    {
                        _secondCharProbs[i][j] = (double)(secondCharCounts[i][j] + firstCharCounts[j]) / denom;
                    }
                }
                if (type2 == CodecCharacterType.Whitespace || type2 == CodecCharacterType.Punctuation)
                {
                    // Factor final char probabilities into all whitespace and punctuation probabilities
                    int denom = numSecondChars[i] + numFinalChars;
                    if (denom > 0)
                    {
                        _secondCharProbs[i][j] = (double)(secondCharCounts[i][j] + finalCharCounts[i]) / denom;
                    }
                }
                else
                {
                    int denom = numSecondChars[i];
                    if (denom > 0)
                    {
                        _secondCharProbs[i][j] = (double)secondCharCounts[i][j] / denom;
                    }
                }
            }
        }

        Console.WriteLine("numSecondChars: " + numSecondChars);
    }

    private void CalculateCululativeProbs()
    {
        _firstCharCumulativeProbs = new(_codec.Count);
        _firstCharCumulativeProbs.Add(_firstCharProbs[0]);
        for (var i = 1; i < _firstCharProbs.Count; i++)
        {
            _firstCharCumulativeProbs.Add(_firstCharCumulativeProbs[i - 1] + _firstCharProbs[i]);
        }

        _secondCharCumulativeProbs = new(_codec.Count);
        for (var i = 0; i < _secondCharProbs.Count; i++)
        {
            _secondCharCumulativeProbs.Add(new(_codec.Count));
            _secondCharCumulativeProbs[i].Add(_secondCharProbs[i][0]);
            for (var j = 1; j < _secondCharProbs.Count; j++)
            {
                _secondCharCumulativeProbs[i].Add(_secondCharCumulativeProbs[i][j - 1] + _secondCharProbs[i][j]);
            }
        }
    }

    private static Random randomGenerator = new();

    private static int SampleDistribution(List<double> distribution)
    {
        double p = Math.Max(0.000001, randomGenerator.NextDouble());
        int index = distribution.BinarySearch(p);

        // BinarySearch returns "a negative number that is the bitwise complement of the
        // index of the next element that is larger than item or, if there is no larger element,
        // the bitwise complement of Count." according to C# docs.
        if (index < 0)
        {
            index = ~index;
        }
        if (index >= distribution.Count)
        {
            index = distribution.Count - 1;
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

        return _codec.GetCharacter(index);
    }

    public CodecCharacter SampleCharacter(char previous)
    {
        int prevIndex = _codec.GetCharacterIndex(previous);
        int index = SampleDistribution(_secondCharCumulativeProbs[prevIndex]);

        return _codec.GetCharacter(index);
    }
}