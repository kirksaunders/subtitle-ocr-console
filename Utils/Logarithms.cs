namespace subtitle_ocr_console.Utils;

public static class Logarithms
{
    public static readonly float LOG_0 = MathF.Log(0.0f);
    public static readonly float LOG_1 = MathF.Log(1.0f);

    public static float LogAddExp(float a, float b)
    {
        if (a == LOG_0)
        {
            return b;
        }
        else if (b == LOG_0)
        {
            return a;
        }

        if (a > b)
        {
            return a + MathF.Log(1.0f + MathF.Exp(b - a));
        }
        else
        {
            return b + MathF.Log(1.0f + MathF.Exp(a - b));
        }
    }

    public static float LogSubExp(float a, float b)
    {
        if (a == LOG_0 || a < b)
        {
            return LOG_0; // Can't represent negative in log-space
        }
        else if (b == LOG_0)
        {
            return a;
        }

        return a + MathF.Log(1.0f - MathF.Exp(b - a));
    }
}