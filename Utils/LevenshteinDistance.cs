namespace subtitle_ocr_console.Utils;

public static class LevenshteinDistance
{
    // Source: https://en.wikipedia.org/wiki/Levenshtein_distance#Iterative_with_two_matrix_rows
    public static int Distance(string s, string t)
    {
        int[] v0 = new int[t.Length + 1];
        int[] v1 = new int[t.Length + 1];

        // initialize v0 (the previous row of distances)
        // this row is A[0][i]: edit distance from an empty s to t;
        // that distance is the number of characters to append to  s to make t.
        for (var i = 0; i <= t.Length; i++)
        {
            v0[i] = i;
        }

        for (var i = 0; i < s.Length; i++)
        {
            // calculate v1 (current row distances) from the previous row v0

            // first element of v1 is A[i + 1][0]
            //   edit distance is delete (i + 1) chars from s to match empty t
            v1[0] = i + 1;

            // use formula to fill in the rest of the row
            for (var j = 0; j < t.Length; j++)
            {
                // calculating costs for A[i + 1][j + 1]
                var deletionCost = v0[j + 1] + 1;
                var insertionCost = v1[j] + 1;
                var substitutionCost = v0[j];
                if (s[i] != t[j])
                {
                    substitutionCost++;
                }

                v1[j + 1] = Math.Min(deletionCost, Math.Min(insertionCost, substitutionCost));
            }

            // copy v1 (current row) to v0 (previous row) for next iteration
            // since data in v1 is always invalidated, a swap without copy could be more efficient
            // Abusing C# tuple syntax a little here
            (v0, v1) = (v1, v0);
        }

        // after the last swap, the results of v1 are now in v0
        return v0[t.Length];
    }
}