namespace SpriteLab.Core;

public sealed class PlaybackClock
{
    private readonly PlaybackCursor _cursor;
    private readonly double[] _durations;
    private readonly double _cycle;
    private readonly double[] _starts;
    public double Duration => _cycle;
    public double PositionMs => _cursor.HasEnded ? _cycle : _starts[_cursor.Position] + ElapsedInFrame;
    public double ElapsedInFrame { get; private set; }
    public PlaybackClock(PlaybackCursor cursor, IEnumerable<double> durations)
    {
        _cursor = cursor;
        _durations = durations.ToArray();
        if (_durations.Length != cursor.Count ||
            _durations.Any(d => !double.IsFinite(d) || d <= 0))
            throw new ArgumentException("Invalid playback durations.");
        _starts = new double[_durations.Length];
        for (var i = 1; i < _starts.Length; i++) _starts[i] = _starts[i - 1] + _durations[i - 1];
        _cycle = _starts[^1] + _durations[^1];
        if (!double.IsFinite(_cycle)) throw new ArgumentException("Invalid cycle duration.");
    }

    public void Reset() => ElapsedInFrame = 0;

    public void Seek(double milliseconds)
    {
        if (!double.IsFinite(milliseconds)) throw new ArgumentOutOfRangeException(nameof(milliseconds));
        var target = Math.Clamp(milliseconds, 0, _cycle);
        var index = Array.BinarySearch(_starts, target);
        if (index < 0) index = ~index - 1;
        index = Math.Clamp(index, 0, _starts.Length - 1);
        _cursor.Pause();
        _cursor.Step(index - _cursor.Position);
        // Keep the final frame visible, without storing a full frame duration that would immediately advance.
        ElapsedInFrame = Math.Min(target - _starts[index], Math.BitDecrement(_durations[index]));
    }

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
