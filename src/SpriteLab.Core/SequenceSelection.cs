namespace SpriteLab.Core;

public static class SequenceSelection
{
    // Keep current occurrence when selecting its source cell; otherwise use first matching occurrence.
    public static int? Find(IReadOnlyList<int> cells, int current, int cell)
    {
        if (current >= 0 && current < cells.Count && cells[current] == cell) return current;
        for (var i = 0; i < cells.Count; i++)
            if (cells[i] == cell) return i;
        return null;
    }
}
