namespace SpriteLab.Core;

public static class PreviewTiming
{
    public const int DefaultFps = 8;
    public const int MaxDurationMs = 60000;

    public static double Duration(int fps, int? overrideMs)
    {
        if (fps is < 1 or > 60) throw new ArgumentOutOfRangeException(nameof(fps));
        if (overrideMs is < 1 or > MaxDurationMs)
            throw new ArgumentOutOfRangeException(nameof(overrideMs));
        return overrideMs ?? (1000.0 / fps);
    }
}
