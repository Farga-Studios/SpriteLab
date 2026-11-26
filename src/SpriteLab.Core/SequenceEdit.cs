namespace SpriteLab.Core;

public static class SequenceEdit
{
    public static int[] Parse(string text, int cellCount)
    {
        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 4096) throw new ArgumentException("Introduce entre 1 y 4096 celdas.");
        var result = new int[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out var cell) || cell < 1 || cell > cellCount)
                throw new ArgumentException($"Celda inválida: {parts[i]}. Rango 1–{cellCount}.");
            result[i] = cell - 1;
        }
        return result;
    }

    public static int?[]? Remap(int[] oldCells, int?[]? oldTimes, int[] newCells)
    {
        if (oldTimes is null) return null;
        if (oldCells.Length != oldTimes.Length) throw new ArgumentException("Timing length mismatch.");
        var map = new Dictionary<int, Queue<int?>>();
        for (var i = 0; i < oldCells.Length; i++)
        {
            if (!map.TryGetValue(oldCells[i], out var queue)) map[oldCells[i]] = queue = new();
            queue.Enqueue(oldTimes[i]);
        }
        var result = newCells.Select(c => map.TryGetValue(c, out var q) && q.Count > 0 ? q.Dequeue() : null).ToArray();
        return result.Any(t => t.HasValue) ? result : null;
    }
}
