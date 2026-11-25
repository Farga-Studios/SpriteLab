namespace SpriteLab.Core;

// Occurrence-based cursor: repeated references remain distinct playback positions.
public sealed class PlaybackCursor
{
    private readonly int[] _cells;
    public int Position { get; private set; }
    public int Count => _cells.Length;
    public int Cell => _cells[Position];
    public bool IsPlaying { get; private set; }

    public PlaybackCursor(IEnumerable<int> cells)
    {
        _cells = cells.ToArray();
        if (_cells.Length == 0 || _cells.Any(c => c < 0))
            throw new ArgumentException("Playback requires valid cells.");
    }

    public void Play() => IsPlaying = true;
    public void Pause() => IsPlaying = false;
    public void Stop() { IsPlaying = false; Position = 0; }

    public void Step(int delta) =>
        Position = (int)Math.Clamp((long)Position + delta, 0, Count - 1);

    public bool Tick(bool loop)
    {
        if (!IsPlaying) return false;
        if (Position < Count - 1) { Position++; return true; }
        if (loop) { Position = 0; return true; }
        Pause();
        return false;
    }
}
