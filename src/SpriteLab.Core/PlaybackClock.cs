namespace SpriteLab.Core;

public sealed class PlaybackClock
{
    private readonly PlaybackCursor _cursor;
    private readonly double[] _durations;
    private readonly double _cycle;
    public double ElapsedInFrame { get; private set; }
    public PlaybackClock(PlaybackCursor cursor, IEnumerable<double> durations)
    {
        _cursor = cursor;
        _durations = durations.ToArray();
        if (_durations.Length != cursor.Count ||
            _durations.Any(d => !double.IsFinite(d) || d <= 0))
            throw new ArgumentException("Invalid playback durations.");
        _cycle = _durations.Sum();
        if (!double.IsFinite(_cycle)) throw new ArgumentException("Invalid cycle duration.");
    }

    public void Reset() => ElapsedInFrame = 0;

    public bool Advance(double milliseconds, bool loop)
    {
        if (!double.IsFinite(milliseconds) || milliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(milliseconds));
        if (!_cursor.IsPlaying) return false;
        // A complete cycle starting at any occurrence returns to that occurrence.
        if (loop) milliseconds %= _cycle;
        ElapsedInFrame += milliseconds;
        var changed = false;
        while (ElapsedInFrame >= _durations[_cursor.Position])
        {
            ElapsedInFrame -= _durations[_cursor.Position];
            if (!_cursor.Tick(loop)) { Reset(); break; }
            changed = true;
        }
        return changed;
    }
}
