namespace subtitle_ocr_console.Utils;

public static class Logarithms
{
    public static float LOG_0 = MathF.Log(0.0f);
    public static float LOG_1 = MathF.Log(1.0f);

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
}